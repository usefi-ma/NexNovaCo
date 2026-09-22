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

internal static class HomeProjectsSectionChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        var denied = await anonymous.GetAsync("/dashboard/content/home/projects");
        Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous ProjectsSection editor must challenge.");
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        var editor = await adminClient.GetAsync("/dashboard/content/home/projects");
        var editorHtml = await editor.Content.ReadAsStringAsync();
        Check(editor.StatusCode == HttpStatusCode.OK && editorHtml.Contains("Our Projects") && !editorHtml.Contains("Opening line"), "Admin editor must load saved content.");
        Check(editorHtml.Contains("mud-input") && editorHtml.Contains("\"type\":\"server\""), "ProjectsSection editor must use interactive Mud inputs.");
        Check((await adminClient.GetAsync("/dashboard/content/home/projects")).StatusCode == HttpStatusCode.OK, "Projects editor must support direct refresh.");
        Check(editorHtml.Contains("href=\"/dashboard/content/home/projects\"") && editorHtml.Contains("Home / Projects"), "Projects editor must have its own sidebar link and page context.");
        Check(typeof(HomeProjectsSectionEditModel).GetProperties().Select(x => x.Name).Order().SequenceEqual(new[] { "Description", "Title" }), "Only the two actual intro fields may be editable.");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        ClaimsPrincipal principal;
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "projects-viewer@example.invalid", Email = "projects-viewer@example.invalid" }, viewerPassword)).Succeeded, "CMS non-Admin test setup failed.");
        }
        using var viewerClient = app.NewClient();
        await Login(viewerClient, "projects-viewer@example.invalid", viewerPassword);
        var viewerEditor = await viewerClient.GetAsync("/dashboard/content/home/projects");
        Check(viewerEditor.StatusCode == HttpStatusCode.Redirect && viewerEditor.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin editor access must be denied.");

        var auth = new TestAuthenticationStateProvider(principal);
        var service = new HomeProjectsSectionContentService(factory, auth, options, NullLogger<HomeProjectsSectionContentService>.Instance);
        var initial = await service.GetAsync();
        Check(Same(initial, HomeProjectsSectionDefaults.Content), "Initial CMS copy must exactly match approved defaults, including description.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeProjectsSectionSettings.CountAsync() == 1, "Startup must create exactly one ProjectsSection.");
            Check((await db.HomeProjectsSectionSettings.SingleAsync()).Id == 1, "ProjectsSection must use fixed Id 1.");
        }
        HomeHeroContent heroBefore;
        string welcomeBefore;
        SectionHeading servicesBefore;
        await using (var db = await factory.CreateDbContextAsync()) servicesBefore = (await db.HomeServicesSectionSettings.SingleAsync()).ToContent();
        await using var projectsScope = app.Services.CreateAsyncScope();
        var projectsService = projectsScope.ServiceProvider.GetRequiredService<IProjectsContentService>();
        var projectsPageBefore = System.Text.Json.JsonSerializer.Serialize(await projectsService.GetAsync());
        HomeContent homeBefore;
        await using (var scope = app.Services.CreateAsyncScope())
            homeBefore = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
        await using (var db = await factory.CreateDbContextAsync()) welcomeBefore = System.Text.Json.JsonSerializer.Serialize((await db.HomeWelcomeSettings.SingleAsync()).ToContent());
        await using (var db = await factory.CreateDbContextAsync()) heroBefore = (await db.HomeHeroSettings.SingleAsync()).ToContent();
        var edit = await service.GetForEditAsync();
        edit.Title = "CMS persistence verified";
        edit.Description = "Edited Home Projects introduction for isolated persistence verification.";
        await service.UpdateAsync(edit);
        Check((await service.GetAsync()).Title == edit.Title, "Update must persist into a new context.");
        Check(System.Text.Json.JsonSerializer.Serialize(await projectsService.GetAsync()) == projectsPageBefore, "Edited Home intro must leave public Projects content unchanged.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var updated = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(updated.ProjectsHeading == edit.ToContent() && updated.Projects.SequenceEqual(homeBefore.Projects), "Only the intro may change; every canonical card must stay identical.");
        }
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeHeroSettings.SingleAsync()).ToContent() == heroBefore, "ProjectsSection save must not alter Hero.");
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var publicHtml = await anonymous.GetStringAsync("/");
            Check(publicHtml.Contains(edit.Title) && publicHtml.Contains(edit.Description), "Anonymous Home/refresh must read edited ProjectsSection from SQLite.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeProjectsSectionInitializer.InitializeAsync(db);
            await HomeProjectsSectionInitializer.InitializeAsync(db);
            Check(await db.HomeProjectsSectionSettings.CountAsync() == 1 && (await db.HomeProjectsSectionSettings.SingleAsync()).Title == edit.Title, "Repeated initialization must not overwrite edits or duplicate singleton.");
            Check((await db.HomeProjectsSectionSettings.SingleAsync()).UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5), "Save must set a recent UTC timestamp.");
            db.HomeProjectsSectionSettings.Add(new HomeProjectsSectionSettings { Id = 2 });
            await ExpectAsync<DbUpdateException>(() => db.SaveChangesAsync(), "Database must reject a second singleton id.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var restartClient = restarted.NewClient();
            Check((await restartClient.GetStringAsync("/")).Contains(edit.Title), "New application host/startup must preserve saved copy.");
            Check((await Login(restartClient, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Restart must preserve existing Admin password.");
        }

        foreach (var property in typeof(HomeProjectsSectionEditModel).GetProperties())
        {
            var invalid = HomeProjectsSectionEditModel.FromContent(initial);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every ProjectsSection field must reject whitespace.");
            property.SetValue(invalid, new string('x', property.GetCustomAttributes(typeof(StringLengthAttribute), false).Cast<StringLengthAttribute>().Single().MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every ProjectsSection field must enforce its length boundary.");
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
            await db.HomeProjectsSectionSettings.ExecuteDeleteAsync(); // Only this suite's isolated database.
        Check(Same(await service.GetAsync(), initial), "Missing record must fall back to approved copy.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(), "Editor must report missing data, not silently use defaults.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeProjectsSectionSettings.CountAsync() == 0, "Public fallback must not persist a record.");
            await HomeProjectsSectionInitializer.InitializeAsync(db);
            Check(await db.HomeProjectsSectionSettings.CountAsync() == 1, "Controlled initialization must repair a missing record.");
        }
        var unavailable = new HomeProjectsSectionContentService(new UnavailableFactory(), auth, options, NullLogger<HomeProjectsSectionContentService>.Instance);
        Check(Same(await unavailable.GetAsync(), initial), "Unavailable database must fall back safely.");
        await ExpectAsync<SqliteException>(() => unavailable.UpdateAsync(edit), "Write failures must propagate for safe editor feedback.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.HomeProjectsSectionSettings.ExecuteUpdateAsync(set => set.SetProperty(x => x.Title, ""));
        Check(Same(await service.GetAsync(), initial), "Invalid stored content must use approved defaults.");
        // Razor encodes content; there is no MarkupString/HTML editor.
        var encoded = HomeProjectsSectionEditModel.FromContent(initial);
        encoded.Title = "<script>alert(1)</script>";
        await service.UpdateAsync(encoded);
        var encodedHtml = await anonymous.GetStringAsync("/");
        Check(encodedHtml.Contains("&lt;script&gt;") && !encodedHtml.Contains(encoded.Title), "ProjectsSection text must remain HTML-encoded.");
        await service.UpdateAsync(HomeProjectsSectionEditModel.FromContent(initial));

        Check(System.Text.Json.JsonSerializer.Serialize(await projectsService.GetAsync()) == projectsPageBefore, "Home intro editing must not affect public Projects page content.");
        Check((await anonymous.GetAsync("/projects")).StatusCode == HttpStatusCode.OK, "Projects page must remain anonymous.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(System.Text.Json.JsonSerializer.Serialize((await db.HomeWelcomeSettings.SingleAsync()).ToContent()) == welcomeBefore, "Projects intro writes must not alter Welcome.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var homeAfter = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(homeBefore.Projects.SequenceEqual(homeAfter.Projects) && homeAfter.Projects.Count == 5, "Home project cards, order, copy, images and routes must remain canonical.");
        }
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeServicesSectionSettings.SingleAsync()).ToContent() == servicesBefore, "Projects intro must not modify saved Services settings.");
        await VerifyUpgradeAsync();
    }

    private static async Task VerifyUpgradeAsync()
    {
        // Upgrade the existing Identity/Hero/Welcome/Services schema, preserving credentials and edited content.
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
        await migrator.MigrateAsync("20260922161150_AddHomeServicesSectionSettings");
        await HomeHeroInitializer.InitializeAsync(db);
        await HomeWelcomeInitializer.InitializeAsync(db);
        await HomeServicesSectionInitializer.InitializeAsync(db);
        var servicesIntro = await db.HomeServicesSectionSettings.SingleAsync();
        servicesIntro.Title = "Existing Services edit";
        var welcome = await db.HomeWelcomeSettings.SingleAsync();
        welcome.Title = "Existing Welcome edit";
        var hero = await db.HomeHeroSettings.SingleAsync();
        hero.OpeningLine = "Existing Hero edit";
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
        await HomeProjectsSectionInitializer.InitializeAsync(db);
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync()).PasswordHash == originalHash && await db.UserRoles.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "ProjectsSection upgrade must preserve existing Identity account/hash/role mapping.");
        Check((await db.HomeHeroSettings.SingleAsync()).OpeningLine == "Existing Hero edit", "ProjectsSection migration must preserve Hero edits.");
        Check((await db.HomeWelcomeSettings.SingleAsync()).Title == "Existing Welcome edit", "Projects migration must preserve Welcome edits.");
        Check((await db.HomeServicesSectionSettings.SingleAsync()).Title == "Existing Services edit", "Projects migration must preserve Services intro edits.");
        Check(await db.HomeProjectsSectionSettings.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Additive migration must create ProjectsSection table with no model drift.");
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
