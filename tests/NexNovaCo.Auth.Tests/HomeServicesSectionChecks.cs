using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class HomeServicesSectionChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        var denied = await anonymous.GetAsync("/dashboard/content/home/services");
        Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous ServicesSection editor must challenge.");
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        var editor = await adminClient.GetAsync("/dashboard/content/home/services");
        var editorHtml = await editor.Content.ReadAsStringAsync();
        Check(editor.StatusCode == HttpStatusCode.OK && editorHtml.Contains("Our Services") && !editorHtml.Contains("Opening line"), "Admin editor must load saved content.");
        Check(editorHtml.Contains("mud-input") && editorHtml.Contains("\"type\":\"server\""), "ServicesSection editor must use interactive Mud inputs.");
        Check((await adminClient.GetAsync("/dashboard/content/home/services")).StatusCode == HttpStatusCode.OK, "Services editor must support direct refresh.");
        Check(editorHtml.Contains("href=\"/dashboard/content/home/services\"") && editorHtml.Contains("Home / Services"), "Services editor must have its own sidebar link and page context.");
        Check(typeof(HomeServicesSectionEditModel).GetProperties().Select(x => x.Name).Order().SequenceEqual(new[] { "Description", "Title" }), "Only the two actual intro fields may be editable.");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        ClaimsPrincipal principal;
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "services-viewer@example.invalid", Email = "services-viewer@example.invalid" }, viewerPassword)).Succeeded, "CMS non-Admin test setup failed.");
        }
        using var viewerClient = app.NewClient();
        await Login(viewerClient, "services-viewer@example.invalid", viewerPassword);
        var viewerEditor = await viewerClient.GetAsync("/dashboard/content/home/services");
        Check(viewerEditor.StatusCode == HttpStatusCode.Redirect && viewerEditor.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin editor access must be denied.");

        var auth = new TestAuthenticationStateProvider(principal);
        var service = new HomeServicesSectionContentService(factory, auth, options, NullLogger<HomeServicesSectionContentService>.Instance);
        var initial = await service.GetAsync();
        Check(Same(initial, HomeServicesSectionDefaults.Content), "Initial CMS copy must exactly match approved defaults, including description.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeServicesSectionSettings.CountAsync() == 1, "Startup must create exactly one ServicesSection.");
            Check((await db.HomeServicesSectionSettings.SingleAsync()).Id == 1, "ServicesSection must use fixed Id 1.");
        }
        HomeHeroContent heroBefore;
        string welcomeBefore;
        await using var publicServicesScope = app.Services.CreateAsyncScope();
        var servicesPageBefore = System.Text.Json.JsonSerializer.Serialize(await publicServicesScope.ServiceProvider.GetRequiredService<IServicesContentService>().GetAsync());
        HomeContent homeBefore;
        await using (var scope = app.Services.CreateAsyncScope())
            homeBefore = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
        await using (var db = await factory.CreateDbContextAsync()) welcomeBefore = System.Text.Json.JsonSerializer.Serialize((await db.HomeWelcomeSettings.SingleAsync()).ToContent());
        await using (var db = await factory.CreateDbContextAsync()) heroBefore = (await db.HomeHeroSettings.SingleAsync()).ToContent();
        var edit = await service.GetForEditAsync();
        edit.Title = "CMS persistence verified";
        edit.Description = "Edited Home Services introduction for isolated persistence verification.";
        await service.UpdateAsync(edit);
        Check((await service.GetAsync()).Title == edit.Title, "Update must persist into a new context.");
        Check(System.Text.Json.JsonSerializer.Serialize(await publicServicesScope.ServiceProvider.GetRequiredService<IServicesContentService>().GetAsync()) == servicesPageBefore, "Edited Home intro must leave public Services content unchanged.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var updated = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(updated.ServicesHeading == edit.ToContent() && updated.Services.SequenceEqual(homeBefore.Services), "Only the intro may change; every canonical card must stay identical.");
        }
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeHeroSettings.SingleAsync()).ToContent() == heroBefore, "ServicesSection save must not alter Hero.");
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var publicHtml = await anonymous.GetStringAsync("/");
            Check(publicHtml.Contains(edit.Title) && publicHtml.Contains(edit.Description), "Anonymous Home/refresh must read edited ServicesSection from SQLite.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeServicesSectionInitializer.InitializeAsync(db);
            await HomeServicesSectionInitializer.InitializeAsync(db);
            Check(await db.HomeServicesSectionSettings.CountAsync() == 1 && (await db.HomeServicesSectionSettings.SingleAsync()).Title == edit.Title, "Repeated initialization must not overwrite edits or duplicate singleton.");
            Check((await db.HomeServicesSectionSettings.SingleAsync()).UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5), "Save must set a recent UTC timestamp.");
            db.HomeServicesSectionSettings.Add(new HomeServicesSectionSettings { Id = 2 });
            await ExpectAsync<DbUpdateException>(() => db.SaveChangesAsync(), "Database must reject a second singleton id.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var restartClient = restarted.NewClient();
            Check((await restartClient.GetStringAsync("/")).Contains(edit.Title), "New application host/startup must preserve saved copy.");
            Check((await Login(restartClient, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Restart must preserve existing Admin password.");
        }

        foreach (var property in typeof(HomeServicesSectionEditModel).GetProperties())
        {
            var invalid = HomeServicesSectionEditModel.FromContent(initial);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every ServicesSection field must reject whitespace.");
            property.SetValue(invalid, new string('x', property.GetCustomAttributes(typeof(StringLengthAttribute), false).Cast<StringLengthAttribute>().Single().MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every ServicesSection field must enforce its length boundary.");
        }
        Check((await service.GetAsync()).Title == edit.Title, "Rejected updates must leave saved copy unchanged.");

        auth.User = new ClaimsPrincipal(new ClaimsIdentity());
        await ExpectAsync<UnauthorizedAccessException>(() => service.GetForEditAsync(), "Anonymous direct editor reads must be denied.");
        await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Anonymous direct service update must be denied.");
        auth.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "viewer")], "test"));
        await ExpectAsync<UnauthorizedAccessException>(() => service.GetForEditAsync(), "Non-Admin direct editor reads must be denied.");
        await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Non-Admin direct service update must be denied.");
        auth.User = principal;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            await users.RemoveFromRoleAsync(admin, "Admin");
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Stale Admin circuit must not save after role removal.");
            await users.AddToRoleAsync(admin, "Admin");
            await users.UpdateSecurityStampAsync(admin);
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Revoked security stamp must block writes immediately.");
            auth.User = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
        }
        // Read failures are safe fallbacks, not hidden writes or editor overwrite opportunities.
        await using (var db = await factory.CreateDbContextAsync())
            await db.HomeServicesSectionSettings.ExecuteDeleteAsync(); // Only this suite's isolated database.
        Check(Same(await service.GetAsync(), initial), "Missing record must fall back to approved copy.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(), "Editor must report missing data, not silently use defaults.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeServicesSectionSettings.CountAsync() == 0, "Public fallback must not persist a record.");
            await HomeServicesSectionInitializer.InitializeAsync(db);
            Check(await db.HomeServicesSectionSettings.CountAsync() == 1, "Controlled initialization must repair a missing record.");
        }
        var unavailable = new HomeServicesSectionContentService(new UnavailableFactory(), auth, options, NullLogger<HomeServicesSectionContentService>.Instance);
        Check(Same(await unavailable.GetAsync(), initial), "Unavailable database must fall back safely.");
        await ExpectAsync<SqliteException>(() => unavailable.UpdateAsync(edit), "Write failures must propagate for safe editor feedback.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.HomeServicesSectionSettings.ExecuteUpdateAsync(set => set.SetProperty(x => x.Title, ""));
        Check(Same(await service.GetAsync(), initial), "Invalid stored content must use approved defaults.");
        // Razor encodes content; there is no MarkupString/HTML editor.
        var encoded = HomeServicesSectionEditModel.FromContent(initial);
        encoded.Title = "<script>alert(1)</script>";
        await service.UpdateAsync(encoded);
        var encodedHtml = await anonymous.GetStringAsync("/");
        Check(encodedHtml.Contains("&lt;script&gt;") && !encodedHtml.Contains(encoded.Title), "ServicesSection text must remain HTML-encoded.");
        await service.UpdateAsync(HomeServicesSectionEditModel.FromContent(initial));

        Check(System.Text.Json.JsonSerializer.Serialize(await publicServicesScope.ServiceProvider.GetRequiredService<IServicesContentService>().GetAsync()) == servicesPageBefore, "Home intro editing must not affect public Services page content.");
        Check((await anonymous.GetAsync("/services")).StatusCode == HttpStatusCode.OK, "Services page must remain anonymous.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(System.Text.Json.JsonSerializer.Serialize((await db.HomeWelcomeSettings.SingleAsync()).ToContent()) == welcomeBefore, "Services intro writes must not alter Welcome.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var homeAfter = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(homeBefore.Services.SequenceEqual(homeAfter.Services) && homeAfter.Services.Count == 5, "Home service cards, order, copy and icons must remain canonical.");
        }
        await VerifyUpgradeAsync();
    }

    private static async Task VerifyUpgradeAsync()
    {
        // Upgrade the existing Identity/Hero/Welcome schema, preserving credentials and edited content.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(connection));
        await using var provider = services.BuildServiceProvider();
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260921223434_AddHomeWelcomeSettings");
        await HistoricalHomeFixture.InitializeAsync(db);
        await db.Database.ExecuteSqlRawAsync("UPDATE HomeWelcomeSettings SET Title=\'Existing Welcome edit\';");
        await db.Database.ExecuteSqlRawAsync("UPDATE HomeHeroSettings SET OpeningLine=\'Existing Hero edit\';");
        await db.SaveChangesAsync();
        var existing = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "upgrade@example.invalid", Email = "upgrade@example.invalid" };
        existing.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(existing, NewPassword());
        var originalHash = existing.PasswordHash;
        var role = new IdentityRole("Admin") { Id = Guid.NewGuid().ToString() };
        db.Users.Add(existing);
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = existing.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        await migrator.MigrateAsync();
        await HomeServicesSectionInitializer.InitializeAsync(db);
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync()).PasswordHash == originalHash && await db.UserRoles.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "ServicesSection upgrade must preserve existing Identity account/hash/role mapping.");
        Check((await db.HomeHeroSettings.SingleAsync()).OpeningLine == "Existing Hero edit", "ServicesSection migration must preserve Hero edits.");
        Check((await db.HomeWelcomeSettings.SingleAsync()).Title == "Existing Welcome edit", "Services migration must preserve Welcome edits.");
        Check(await db.HomeServicesSectionSettings.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Additive migration must create ServicesSection table with no model drift.");
    }

    private static bool Same(SectionHeading left, SectionHeading right) => left == right;

    private static async Task ExpectAsync<T>(Func<Task> action, string message) where T : Exception
    {
        try { await action(); }
        catch (T) { Check(true, message); return; }
        Check(false, message);
    }

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public ClaimsPrincipal User { get; set; } = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
    }

    private sealed class UnavailableFactory : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => throw new SqliteException("Simulated unavailable test database.", 14);
    }
}
