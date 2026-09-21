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

internal static class HomeHeroChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        var denied = await anonymous.GetAsync("/dashboard/home/hero");
        Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous Hero editor must challenge.");
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        var editor = await adminClient.GetAsync("/dashboard/home/hero");
        var editorHtml = await editor.Content.ReadAsStringAsync();
        Check(editor.StatusCode == HttpStatusCode.OK && editorHtml.Contains("Opening line") && editorHtml.Contains("Smart Software"), "Admin editor must load saved content.");
        Check(editorHtml.Contains("mud-input") && editorHtml.Contains("\"type\":\"server\""), "Hero editor must use interactive Mud inputs.");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        ClaimsPrincipal principal;
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "hero-viewer@example.invalid", Email = "hero-viewer@example.invalid" }, viewerPassword)).Succeeded, "CMS non-Admin test setup failed.");
        }
        using var viewerClient = app.NewClient();
        await Login(viewerClient, "hero-viewer@example.invalid", viewerPassword);
        var viewerEditor = await viewerClient.GetAsync("/dashboard/home/hero");
        Check(viewerEditor.StatusCode == HttpStatusCode.Redirect && viewerEditor.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin editor access must be denied.");

        var auth = new TestAuthenticationStateProvider(principal);
        var service = new HomeHeroContentService(factory, auth, options, NullLogger<HomeHeroContentService>.Instance);
        var initial = await service.GetAsync();
        Check(initial == HomeHeroDefaults.Content, "Initial CMS copy must exactly match approved defaults, including relative CTA.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeHeroSettings.CountAsync() == 1, "Startup must create exactly one Hero.");
            Check((await db.HomeHeroSettings.SingleAsync()).Id == 1, "Hero must use fixed Id 1.");
        }
        var edit = await service.GetForEditAsync();
        edit.OpeningLine = "CMS persistence verified";
        edit.CtaLabel = "Explore our projects";
        edit.CtaHref = "/projects";
        await service.UpdateAsync(edit);
        Check((await service.GetAsync()).OpeningLine == edit.OpeningLine, "Update must persist into a new context.");
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var publicHtml = await anonymous.GetStringAsync("/");
            Check(publicHtml.Contains(edit.OpeningLine) && publicHtml.Contains("href=\"/projects\""), "Anonymous Home/refresh must read edited Hero from SQLite.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeHeroInitializer.InitializeAsync(db);
            await HomeHeroInitializer.InitializeAsync(db);
            Check(await db.HomeHeroSettings.CountAsync() == 1 && (await db.HomeHeroSettings.SingleAsync()).OpeningLine == edit.OpeningLine, "Repeated initialization must not overwrite edits or duplicate singleton.");
            Check((await db.HomeHeroSettings.SingleAsync()).UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5), "Save must set a recent UTC timestamp.");
            db.HomeHeroSettings.Add(new HomeHeroSettings { Id = 2 });
            await ExpectAsync<DbUpdateException>(() => db.SaveChangesAsync(), "Database must reject a second singleton id.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var restartClient = restarted.NewClient();
            Check((await restartClient.GetStringAsync("/")).Contains(edit.OpeningLine), "New application host/startup must preserve saved copy.");
            Check((await Login(restartClient, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Restart must preserve existing Admin password.");
        }

        foreach (var unsafeHref in new[] { "javascript:alert(1)", "https://example.invalid", "//example.invalid", "/\\example.invalid", "/%2f%2fexample.invalid", "data:text/html,test", "/services?next=https://example.invalid", "/admin/login", "../services", "services\n" })
        {
            var invalid = HomeHeroEditModel.FromContent(initial);
            invalid.CtaHref = unsafeHref;
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Unsafe/non-public CTA must be rejected.");
        }
        foreach (var property in typeof(HomeHeroEditModel).GetProperties())
        {
            var invalid = HomeHeroEditModel.FromContent(initial);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every Hero field must reject whitespace.");
            property.SetValue(invalid, new string('x', property.GetCustomAttributes(typeof(StringLengthAttribute), false).Cast<StringLengthAttribute>().Single().MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every Hero field must enforce its length boundary.");
        }
        Check((await service.GetAsync()).OpeningLine == edit.OpeningLine, "Rejected updates must leave saved copy unchanged.");

        auth.User = new ClaimsPrincipal(new ClaimsIdentity());
        await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Anonymous direct service update must be denied.");
        auth.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "viewer")], "test"));
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
            await db.HomeHeroSettings.ExecuteDeleteAsync(); // Only this suite's isolated database.
        Check(await service.GetAsync() == initial, "Missing record must fall back to approved copy.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(), "Editor must report missing data, not silently use defaults.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeHeroSettings.CountAsync() == 0, "Public fallback must not persist a record.");
            await HomeHeroInitializer.InitializeAsync(db);
            Check(await db.HomeHeroSettings.CountAsync() == 1, "Controlled initialization must repair a missing record.");
        }
        var unavailable = new HomeHeroContentService(new UnavailableFactory(), auth, options, NullLogger<HomeHeroContentService>.Instance);
        Check(await unavailable.GetAsync() == initial, "Unavailable database must fall back safely.");
        await ExpectAsync<SqliteException>(() => unavailable.UpdateAsync(edit), "Write failures must propagate for safe editor feedback.");
        // Razor encodes content; there is no MarkupString/HTML editor.
        var encoded = HomeHeroEditModel.FromContent(initial);
        encoded.OpeningLine = "<script>alert(1)</script>";
        await service.UpdateAsync(encoded);
        var encodedHtml = await anonymous.GetStringAsync("/");
        Check(encodedHtml.Contains("&lt;script&gt;") && !encodedHtml.Contains(encoded.OpeningLine), "Hero text must remain HTML-encoded.");
        await service.UpdateAsync(HomeHeroEditModel.FromContent(initial));

        await VerifyUpgradeAsync();
    }

    private static async Task VerifyUpgradeAsync()
    {
        // Simulate an existing Phase 1 database with an Admin, then apply only the additive migration.
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
        await migrator.MigrateAsync("20260921020020_InitialIdentity");
        var existing = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "upgrade@example.invalid", Email = "upgrade@example.invalid" };
        existing.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(existing, NewPassword());
        var originalHash = existing.PasswordHash;
        var role = new IdentityRole("Admin") { Id = Guid.NewGuid().ToString() };
        db.Users.Add(existing);
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = existing.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        await migrator.MigrateAsync();
        await HomeHeroInitializer.InitializeAsync(db);
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync()).PasswordHash == originalHash && await db.UserRoles.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "Hero upgrade must preserve existing Identity account/hash/role mapping.");
        Check(await db.HomeHeroSettings.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Additive migration must create Hero table with no model drift.");
    }

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
