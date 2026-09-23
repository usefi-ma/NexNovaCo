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

internal static class HomeTeamSectionChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        var denied = await anonymous.GetAsync("/dashboard/content/home/team");
        Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous Team editor must challenge.");
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        var editor = await adminClient.GetAsync("/dashboard/content/home/team");
        var editorHtml = await editor.Content.ReadAsStringAsync();
        Check(editor.StatusCode == HttpStatusCode.OK && editorHtml.Contains("Our Team") && !editorHtml.Contains("Opening line"), "Admin editor must load saved content.");
        Check(editorHtml.Contains("mud-input") && editorHtml.Contains("\"type\":\"server\""), "Team editor must use interactive Mud inputs.");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        ClaimsPrincipal principal;
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "team-viewer@example.invalid", Email = "team-viewer@example.invalid" }, viewerPassword)).Succeeded, "CMS non-Admin test setup failed.");
        }
        using var viewerClient = app.NewClient();
        await Login(viewerClient, "team-viewer@example.invalid", viewerPassword);
        var viewerEditor = await viewerClient.GetAsync("/dashboard/content/home/team");
        Check(viewerEditor.StatusCode == HttpStatusCode.Redirect && viewerEditor.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin editor access must be denied.");

        var auth = new TestAuthenticationStateProvider(principal);
        var service = new HomeTeamSectionContentService(factory, auth, options, NullLogger<HomeTeamSectionContentService>.Instance);
        var initial = await service.GetAsync();
        Check(Same(initial, HomeTeamSectionDefaults.Content), "Initial CMS copy must exactly match approved defaults, including relative CTA.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeTeamSectionSettings.CountAsync() == 1, "Startup must create exactly one Team.");
            Check((await db.HomeTeamSectionSettings.SingleAsync()).Id == 1, "Team must use fixed Id 1.");
        }
        Check((await adminClient.GetAsync("/dashboard/content/home/team")).StatusCode == HttpStatusCode.OK, "Team editor supports direct refresh.");
        Check(editorHtml.Contains("href=\"/dashboard/content/home/team\"") && editorHtml.Contains("Home / Team"), "Team has its own sidebar link and page context.");
        Check(typeof(HomeTeamSectionEditModel).GetProperties().Select(x => x.Name).Order().SequenceEqual(new[] { "CtaHref", "CtaLabel", "Description", "Highlight", "Introduction", "Title" }), "Only actual Team intro fields may be editable.");
        await using var teamScope = app.Services.CreateAsyncScope();
        var teamPageBefore = System.Text.Json.JsonSerializer.Serialize(await teamScope.ServiceProvider.GetRequiredService<ITeamContentService>().GetAsync());
        HomeContent homeBefore;
        await using (var scope = app.Services.CreateAsyncScope())
            homeBefore = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
        HomeHeroContent heroBefore;
        await using (var db = await factory.CreateDbContextAsync()) heroBefore = (await db.HomeHeroSettings.SingleAsync()).ToContent();
        var edit = await service.GetForEditAsync();
        edit.Title = "CMS persistence verified";
        edit.Introduction = "An edited team introduction.";
        edit.Highlight = "An edited highlighted sentence.";
        edit.Description = "An edited Team description.";

        edit.CtaLabel = "Explore our projects";
        edit.CtaHref = "/projects";
        await service.UpdateAsync(edit);
        Check(await service.GetAsync() == edit.ToContent(), "All six values must persist into a new context.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var changed = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(changed.Team == edit.ToContent(), "Home must use the edited Team intro.");
            Check(changed.Members.SequenceEqual(homeBefore.Members) && changed.Members.Count == 4, "Home member identity/order/copy/images/routes/social data must stay canonical.");
        }
        Check(System.Text.Json.JsonSerializer.Serialize(await teamScope.ServiceProvider.GetRequiredService<ITeamContentService>().GetAsync()) == teamPageBefore, "Home edits must not change public Team-page content.");
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeHeroSettings.SingleAsync()).ToContent() == heroBefore, "Team save must not alter Hero.");
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var publicHtml = await anonymous.GetStringAsync("/");
            Check(publicHtml.Contains(edit.Title) && publicHtml.Contains("href=\"/projects\""), "Anonymous Home/refresh must read edited Team from SQLite.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeTeamSectionInitializer.InitializeAsync(db);
            await HomeTeamSectionInitializer.InitializeAsync(db);
            Check(await db.HomeTeamSectionSettings.CountAsync() == 1 && (await db.HomeTeamSectionSettings.SingleAsync()).Title == edit.Title, "Repeated initialization must not overwrite edits or duplicate singleton.");
            Check((await db.HomeTeamSectionSettings.SingleAsync()).UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5), "Save must set a recent UTC timestamp.");
            db.HomeTeamSectionSettings.Add(new HomeTeamSectionSettings { Id = 2 });
            await ExpectAsync<DbUpdateException>(() => db.SaveChangesAsync(), "Database must reject a second singleton id.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var restartClient = restarted.NewClient();
            Check((await restartClient.GetStringAsync("/")).Contains(edit.Title), "New application host/startup must preserve saved copy.");
            Check((await Login(restartClient, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Restart must preserve existing Admin password.");
        }

        foreach (var unsafeHref in new[] { "javascript:alert(1)", "https://example.invalid", "//example.invalid", "/\\example.invalid", "/%2f%2fexample.invalid", "data:text/html,test", "/services?next=https://example.invalid", "/admin/login", "../services", "services\n" })
        {
            var invalid = HomeTeamSectionEditModel.FromContent(initial);
            invalid.CtaHref = unsafeHref;
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Unsafe/non-public CTA must be rejected.");
        }
        foreach (var property in typeof(HomeTeamSectionEditModel).GetProperties())
        {
            var invalid = HomeTeamSectionEditModel.FromContent(initial);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every Team field must reject whitespace.");
            property.SetValue(invalid, new string('x', property.GetCustomAttributes(typeof(StringLengthAttribute), false).Cast<StringLengthAttribute>().Single().MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every Team field must enforce its length boundary.");
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
            await db.HomeTeamSectionSettings.ExecuteDeleteAsync(); // Only this suite's isolated database.
        Check(Same(await service.GetAsync(), initial), "Missing record must fall back to approved copy.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(), "Editor must report missing data, not silently use defaults.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeTeamSectionSettings.CountAsync() == 0, "Public fallback must not persist a record.");
            await HomeTeamSectionInitializer.InitializeAsync(db);
            Check(await db.HomeTeamSectionSettings.CountAsync() == 1, "Controlled initialization must repair a missing record.");
        }
        var unavailable = new HomeTeamSectionContentService(new UnavailableFactory(), auth, options, NullLogger<HomeTeamSectionContentService>.Instance);
        Check(Same(await unavailable.GetAsync(), initial), "Unavailable database must fall back safely.");
        await ExpectAsync<SqliteException>(() => unavailable.UpdateAsync(edit), "Write failures must propagate for safe editor feedback.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.HomeTeamSectionSettings.ExecuteUpdateAsync(set => set.SetProperty(x => x.CtaHref, "javascript:alert(1)"));
        Check(Same(await service.GetAsync(), initial), "Invalid stored CTA must use safe defaults, not render unsafe actions.");
        foreach (var route in new[] { "/", "about", "/services", "/contact", "/projects/nexconnect", "/team/emilyjohnson" })
        {
            var valid = HomeTeamSectionEditModel.FromContent(initial);
            valid.CtaHref = route;
            await service.UpdateAsync(valid);
            Check((await service.GetAsync()).CtaHref == route, "Approved public routes must be accepted.");
        }
        // Razor encodes content; there is no MarkupString/HTML editor.
        var encoded = HomeTeamSectionEditModel.FromContent(initial);
        encoded.Title = "<script>alert(1)</script>";
        await service.UpdateAsync(encoded);
        var encodedHtml = await anonymous.GetStringAsync("/");
        Check(encodedHtml.Contains("&lt;script&gt;") && !encodedHtml.Contains(encoded.Title), "Team text must remain HTML-encoded.");
        await service.UpdateAsync(HomeTeamSectionEditModel.FromContent(initial));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var after = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(after.Hero == homeBefore.Hero && after.ServicesHeading == homeBefore.ServicesHeading && after.ProjectsHeading == homeBefore.ProjectsHeading && System.Text.Json.JsonSerializer.Serialize(after.Welcome) == System.Text.Json.JsonSerializer.Serialize(homeBefore.Welcome), "Team writes must preserve all earlier Home settings.");
        }
        Check((await anonymous.GetAsync("/team")).StatusCode == HttpStatusCode.OK, "Public Team page remains anonymous.");
        await VerifyUpgradeAsync();
    }

    private static async Task VerifyUpgradeAsync()
    {
        // Upgrade the previous schema, preserving Identity and all four existing edited Home slices.
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
        await migrator.MigrateAsync("20260922163222_AddHomeProjectsSectionSettings");
        await HistoricalHomeFixture.InitializeAsync(db);
        await HomeServicesSectionInitializer.InitializeAsync(db);
        await HomeProjectsSectionInitializer.InitializeAsync(db);
        await db.Database.ExecuteSqlRawAsync("UPDATE HomeWelcomeSettings SET Title=\'Existing Welcome edit\';");
        (await db.HomeServicesSectionSettings.SingleAsync()).Title = "Existing Services edit";
        (await db.HomeProjectsSectionSettings.SingleAsync()).Title = "Existing Projects edit";
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
        await HomeTeamSectionInitializer.InitializeAsync(db);
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync()).PasswordHash == originalHash && await db.UserRoles.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "Team upgrade must preserve existing Identity account/hash/role mapping.");
        Check((await db.HomeHeroSettings.SingleAsync()).OpeningLine == "Existing Hero edit", "Team migration must preserve Hero edits.");
        Check((await db.HomeWelcomeSettings.SingleAsync()).Title == "Existing Welcome edit" && (await db.HomeServicesSectionSettings.SingleAsync()).Title == "Existing Services edit" && (await db.HomeProjectsSectionSettings.SingleAsync()).Title == "Existing Projects edit", "Team migration preserves all existing Home settings.");
        Check(await db.HomeTeamSectionSettings.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Additive migration must create Team table with no model drift.");
    }

    private static bool Same(TeamSectionContent left, TeamSectionContent right) => left == right;

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
