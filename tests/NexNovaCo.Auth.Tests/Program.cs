using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Components.Account;

namespace NexNovaCo.Auth.Tests;

internal static class AuthChecks
{
    private static int _checks;
    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
    internal static string NewPassword() => "Aa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    private static Dictionary<string, string> Form(string html)
    {
        var result = new Dictionary<string, string>();
        foreach (Match input in Regex.Matches(html, "<input[^>]*type=\"hidden\"[^>]*>"))
        {
            var name = Regex.Match(input.Value, "name=\"([^\"]+)\"").Groups[1].Value;
            var value = Regex.Match(input.Value, "value=\"([^\"]*)\"").Groups[1].Value;
            if (name.Length > 0) result[name] = WebUtility.HtmlDecode(value);
        }
        return result;
    }
    internal static async Task<HttpResponseMessage> Login(HttpClient client, string email, string password, string query = "")
    {
        var html = await client.GetStringAsync("/admin/login" + query);
        var form = Form(html);
        Check(form.ContainsKey("__RequestVerificationToken"), "Login must include antiforgery token.");
        form["Input.Email"] = email;
        form["Input.Password"] = password;
        return await client.PostAsync("/admin/login" + query, new FormUrlEncodedContent(form));
    }
    private static bool RedirectsTo(HttpResponseMessage response, string path) =>
        response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther &&
        response.Headers.Location is { } uri && (uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString.Split('?')[0]) == path;

    public static async Task Main()
    {
        // Credentials exist only in memory/environment configuration for isolated test databases.
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        foreach (var route in new[] { "/", "/services", "/projects", "/contact" })
        {
            var response = await anonymous.GetAsync(route);
            Check(response.StatusCode == HttpStatusCode.OK, "Public route must stay anonymous: " + route);
            var html = await response.Content.ReadAsStringAsync();
            Check(html.Contains("aria-label=\"Primary\"") && html.Contains("aria-label=\"Footer\""), "Public shell missing: " + route);
        }
        var challenge = await anonymous.GetAsync("/dashboard");
        Check(RedirectsTo(challenge, "/admin/login"), "Anonymous Dashboard must challenge to admin login.");
        Check(!((await challenge.Content.ReadAsStringAsync()).Contains("Edit Home")), "Protected content leaked in anonymous response.");
        var loginHtml = await anonymous.GetStringAsync("/admin/login");
        Check(loginHtml.Contains("<h1") && loginHtml.Contains("Admin login"), "Login page must render.");
        Check(!loginHtml.Contains("\"type\":\"server\""), "Login must be static SSR, not an interactive circuit.");
        Check(!loginHtml.Contains("public-shell.css") && loginHtml.Contains("admin."), "Admin must use its own stylesheet.");
        Check(!loginHtml.Contains("Register") && !loginHtml.Contains("Sign Up"), "Registration must not be exposed.");
        foreach (var route in new[] { "/admin/register", "/Account/Register", "/admin/forgot-password" })
            Check((await anonymous.GetAsync(route)).StatusCode == HttpStatusCode.NotFound, "Out-of-scope account endpoint exists.");

        var missingToken = await anonymous.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["_handler"] = "admin-login", ["Input.Email"] = AuthFactory.Email, ["Input.Password"] = app.Password }));
        Check(missingToken.StatusCode == HttpStatusCode.BadRequest, "Login without antiforgery must fail.");
        var wrongPassword = NewPassword();
        var invalid = await Login(anonymous, AuthFactory.Email, wrongPassword);
        var invalidHtml = await invalid.Content.ReadAsStringAsync();
        Check(invalid.StatusCode == HttpStatusCode.OK && invalidHtml.Contains("Unable to sign in."), "Wrong password must fail generically.");
        Check(!invalidHtml.Contains(wrongPassword), "A failed login must not echo the password into HTML.");
        Check(!invalid.Headers.TryGetValues("Set-Cookie", out var badCookies) || !badCookies.Any(x => x.StartsWith("NexNovaCo.Identity=")), "Failed login issued auth cookie.");
        var unknown = await Login(anonymous, "missing@example.invalid", NewPassword());
        Check((await unknown.Content.ReadAsStringAsync()).Contains("Unable to sign in."), "Unknown user must receive same generic error.");

        var valid = await Login(anonymous, AuthFactory.Email, app.Password, "?returnUrl=https%3A%2F%2Fexample.invalid%2Foutside");
        Check(RedirectsTo(valid, "/dashboard"), "Admin login must use safe Dashboard redirect, not supplied external URL.");
        var cookie = valid.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("NexNovaCo.Identity="));
        Check(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase), "Identity cookie must be HttpOnly/SameSite Lax.");
        Check(!cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase), "Login should not create a persistent remember-me cookie.");
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var dashboard = await anonymous.GetAsync("/dashboard");
            Check(dashboard.StatusCode == HttpStatusCode.OK, "Authenticated direct/refresh Dashboard request must succeed.");
            var html = await dashboard.Content.ReadAsStringAsync();
            Check(html.Contains(AuthFactory.Email) && html.Contains("Edit Home"), "Dashboard must show authenticated identity/content.");
            Check(html.Contains("mud-drawer") && Regex.IsMatch(html, "<button[^>]*aria-label=\"Open administrator menu\""), "Dashboard must include drawer and a native accessible avatar trigger.");
            Check(html.Contains("action=\"/admin/logout\"") && html.Contains("method=\"post\""), "Avatar logout must retain native HTTP POST form.");
            Check(html.Contains("mud-appbar") && !html.Contains("class=\"footer\""), "Dashboard must have a separate Mud layout.");
            Check(html.Contains("\"type\":\"server\""), "Dashboard must use Interactive Server.");
        }
        Check(RedirectsTo(await anonymous.GetAsync("/admin/login"), "/dashboard"), "Existing Admin must not be shown login again.");
        var invalidLogout = await anonymous.PostAsync("/admin/logout", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Check(invalidLogout.StatusCode == HttpStatusCode.BadRequest, "Logout without antiforgery must fail.");
        Check((await anonymous.GetAsync("/dashboard")).StatusCode == HttpStatusCode.OK, "Invalid logout must not clear login.");
        var getLogout = await anonymous.GetAsync("/admin/logout");
        Check(getLogout.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound, "GET logout must not be supported: " + getLogout.StatusCode);
        Check((await anonymous.GetAsync("/dashboard")).StatusCode == HttpStatusCode.OK, "GET logout must not change authentication.");
        var dashboardHtml = await anonymous.GetStringAsync("/dashboard");
        var logout = await anonymous.PostAsync("/admin/logout", new FormUrlEncodedContent(Form(dashboardHtml)));
        Check(RedirectsTo(logout, "/admin/login"), "POST logout must return to login.");
        Check(RedirectsTo(await anonymous.GetAsync("/dashboard"), "/admin/login"), "Dashboard must be inaccessible after logout.");

        string nonAdminPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Check((await db.Database.GetAppliedMigrationsAsync()).Count() == 7, "Expected Identity and six Home settings migrations.");
            Check(!db.Database.HasPendingModelChanges(), "Migration and runtime model must agree.");
            var tables = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table'").ToListAsync();
            Check(new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens", "AspNetRoleClaims" }.All(tables.Contains), "Identity tables missing.");
            Check(tables.All(x => x.StartsWith("AspNet") || x.StartsWith("__EF") || x is "sqlite_sequence" or "HomeHeroSettings" or "HomeWelcomeSettings" or "HomeServicesSectionSettings" or "HomeProjectsSectionSettings" or "HomeTeamSectionSettings" or "HomeStatistics"), "Unexpected schema beyond Identity and approved Home settings.");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await users.FindByEmailAsync(AuthFactory.Email);
            Check(admin is not null && await users.IsInRoleAsync(admin, "Admin"), "Admin must exist and have Admin role.");
            Check(admin!.PasswordHash is { Length: > 20 } && admin.PasswordHash != app.Password && await users.CheckPasswordAsync(admin, app.Password), "Identity must hash and verify the password.");
            var firstHash = admin.PasswordHash;
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["AdminUser:Email"] = AuthFactory.Email, ["AdminUser:Password"] = NewPassword() }).Build();
            await IdentityDatabaseInitializer.InitializeAsync(app.Services, configuration, app.Services.GetRequiredService<IWebHostEnvironment>());
            await db.Entry(admin).ReloadAsync();
            Check(admin.PasswordHash == firstHash && await users.CheckPasswordAsync(admin, app.Password), "Repeated bootstrap must not reset an existing Admin password.");
            Check(await db.Users.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "Bootstrap must be idempotent.");
            var nonAdmin = new ApplicationUser { UserName = "viewer@example.invalid", Email = "viewer@example.invalid" };
            Check((await users.CreateAsync(nonAdmin, nonAdminPassword)).Succeeded, "Test-only non-Admin creation failed.");
            Check(!(await users.CreateAsync(new ApplicationUser { UserName = "duplicate", Email = AuthFactory.Email }, NewPassword())).Succeeded, "Duplicate email must be rejected.");
            Check(!(await users.CreateAsync(new ApplicationUser { UserName = "weak@example.invalid", Email = "weak@example.invalid" }, "weak")).Succeeded, "Weak passwords must be rejected.");
            var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            var provider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
            var validate = typeof(IdentityRevalidatingAuthenticationStateProvider).GetMethod("ValidateAuthenticationStateAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Task<bool> Revalidate() => (Task<bool>)validate.Invoke(provider, [new AuthenticationState(principal), CancellationToken.None])!;
            Check(await Revalidate(), "Current Admin circuit should validate.");
            Check((await users.RemoveFromRoleAsync(admin, "Admin")).Succeeded && !await Revalidate(), "Role removal must invalidate a privileged circuit.");
            Check((await users.AddToRoleAsync(admin, "Admin")).Succeeded, "Test role restoration failed.");
            Check((await users.UpdateSecurityStampAsync(admin)).Succeeded && !await Revalidate(), "Changed security stamp must invalidate an old circuit.");
        }
        using var viewer = app.NewClient();
        Check(RedirectsTo(await Login(viewer, "viewer@example.invalid", nonAdminPassword), "/dashboard"), "Valid non-Admin credentials should authenticate before role enforcement.");
        var denied = await viewer.GetAsync("/dashboard");
        Check(RedirectsTo(denied, "/admin/access-denied") || denied.StatusCode == HttpStatusCode.Forbidden, "Authenticated non-Admin must be denied.");
        Check(!(await denied.Content.ReadAsStringAsync()).Contains("Edit Home"), "Protected content leaked to non-Admin.");

        // Five failures activate standard Identity lockout; no special account detail is exposed.
        using var lockedClient = app.NewClient();
        for (var i = 0; i < 5; i++) await Login(lockedClient, AuthFactory.Email, NewPassword());
        var locked = await Login(lockedClient, AuthFactory.Email, app.Password);
        Check((await locked.Content.ReadAsStringAsync()).Contains("Unable to sign in."), "Locked-out account must not sign in or expose lockout detail.");

        // Guard against future routable admin components accidentally added outside the protected folder.
        var routeTypes = typeof(global::Program).Assembly.GetTypes().Where(type => type.GetCustomAttributes<RouteAttribute>().Any());
        foreach (var type in routeTypes)
        foreach (var route in type.GetCustomAttributes<RouteAttribute>())
        {
            if (route.Template.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase) ||
                route.Template.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            {
                var exception = route.Template is "/admin/login" or "/admin/access-denied";
                Check(exception || (type.GetCustomAttributes<AuthorizeAttribute>().Any(x => x.Roles == "Admin") &&
                    !type.IsDefined(typeof(AllowAnonymousAttribute))), "Admin route lacks mandatory role authorization: " + route.Template);
            }
        }
        // Cookie settings must remain secure independently of a reverse proxy in Production.
        await using var production = new AuthFactory(NewPassword(), "Production");
        using var productionClient = production.NewClient();
        var productionLogin = await Login(productionClient, AuthFactory.Email, production.Password);
        Check(RedirectsTo(productionLogin, "/dashboard"), "Production login failed.");
        Check(productionLogin.Headers.GetValues("Set-Cookie").Any(x => x.StartsWith("NexNovaCo.Identity=") && x.Contains("secure", StringComparison.OrdinalIgnoreCase)), "Production auth cookie must be Secure.");
        Check(!(await productionClient.GetStringAsync("/?verify=foundation")).Contains("Foundation diagnostics"), "Production diagnostics leaked.");
        await using var missingCredentials = new AuthFactory("");
        using var missingClient = missingCredentials.NewClient();
        Check((await missingClient.GetAsync("/admin/login")).StatusCode == HttpStatusCode.OK, "Missing credentials must not prevent Development startup.");
        await using (var scope = missingCredentials.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Check(await db.Users.CountAsync() == 0 && await db.Roles.CountAsync() == 1, "Missing credentials must create the role, not a default user.");
        }
        await HomeHeroChecks.RunAsync();
        await HomeWelcomeChecks.RunAsync();
        await HomeServicesSectionChecks.RunAsync();
        await HomeProjectsSectionChecks.RunAsync();
        await HomeTeamSectionChecks.RunAsync();
        await HomeStatisticsChecks.RunAsync();
        await HomeNavigationChecks.RunAsync();
        Console.WriteLine($"PASS: {_checks} auth/CMS checks (HTTP authentication, roles, migration/bootstrap, six Home CMS slices, persistence/validation/fallback and anonymous public routes). No secrets or hashes printed.");
    }
}

internal sealed class AuthFactory(string password, string environment = "Development", string? databasePath = null) : WebApplicationFactory<global::Program>
{
    public const string Email = "admin@example.invalid";
    public string Password { get; } = password;
    public string DatabasePath { get; } = databasePath ?? Path.Combine(Path.GetTempPath(), "NexNovaCo.Auth.Tests", Guid.NewGuid().ToString("N"), "identity.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.GetFullPath("../../../../../src/NexNovaCo.Web", AppContext.BaseDirectory));
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:IdentityConnection", "Data Source=" + DatabasePath);
        builder.UseSetting("AdminUser:Email", Email);
        builder.UseSetting("AdminUser:Password", Password);
        builder.UseSetting("Identity:InitializeDatabase", "true");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
}
