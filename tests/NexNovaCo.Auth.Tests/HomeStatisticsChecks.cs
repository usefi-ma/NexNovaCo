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

internal static class HomeStatisticsChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        var denied = await anonymous.GetAsync("/dashboard/content/home/statistics");
        Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous Statistics editor must challenge.");
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        var editor = await adminClient.GetAsync("/dashboard/content/home/statistics");
        var editorHtml = await editor.Content.ReadAsStringAsync();
        Check(editor.StatusCode == HttpStatusCode.OK && editorHtml.Contains("PROJECTS") && !editorHtml.Contains("Opening line"), "Admin editor must load saved content.");
        Check(editorHtml.Contains("mud-input") && editorHtml.Contains("\"type\":\"server\""), "Statistics editor must use interactive Mud inputs.");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        ClaimsPrincipal principal;
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "statistics-viewer@example.invalid", Email = "statistics-viewer@example.invalid" }, viewerPassword)).Succeeded, "CMS non-Admin test setup failed.");
        }
        using var viewerClient = app.NewClient();
        await Login(viewerClient, "statistics-viewer@example.invalid", viewerPassword);
        var viewerEditor = await viewerClient.GetAsync("/dashboard/content/home/statistics");
        Check(viewerEditor.StatusCode == HttpStatusCode.Redirect && viewerEditor.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin editor access must be denied.");

        var auth = new TestAuthenticationStateProvider(principal);
        var service = new HomeStatisticsContentService(factory, auth, options, NullLogger<HomeStatisticsContentService>.Instance);
        var initial = await service.GetAsync();
        Check(Same(initial, HomeStatisticsDefaults.Content), "Initial statistics match approved labels/values/order.");
        Check((await adminClient.GetAsync("/dashboard/content/home/statistics")).StatusCode == HttpStatusCode.OK, "Statistics direct refresh works.");
        Check(editorHtml.Contains("href=\"/dashboard/content/home/statistics\"") && editorHtml.Contains("Home / Statistics"), "Sidebar and context identify Statistics.");
        Check(typeof(HomeStatisticEditModel).GetProperties().Select(x => x.Name).Order().SequenceEqual(new[] { "Id", "Label", "Value" }), "Only fixed identity, label and value exist, no invented prefix/suffix.");
        await using var homeScope = app.Services.CreateAsyncScope();
        var homeService = homeScope.ServiceProvider.GetRequiredService<IHomeContentService>();
        var homeBefore = await homeService.GetAsync();
        var edit = await service.GetForEditAsync();
        for (var i = 0; i < edit.Items.Count; i++)
        {
            edit.Items[i].Label = "STAT " + (i + 1);
            edit.Items[i].Value = 725 + i;
        }
        await service.UpdateAsync(edit);
        var saved = await service.GetAsync();
        Check(saved.Select(x => x.Value).SequenceEqual(new[] { 725, 726, 727, 728 }), "One save persists all four numbers.");
        Check(saved.Select(x => x.Label).SequenceEqual(edit.Items.Select(x => x.Label)), "One save persists all four labels.");
        var after = await homeService.GetAsync();
        Check(Same(after.Statistics, saved), "Home must read edited statistics even through an already-used service.");
        Check(after.Hero == homeBefore.Hero && after.Team == homeBefore.Team && after.ServicesHeading == homeBefore.ServicesHeading &&
            after.ProjectsHeading == homeBefore.ProjectsHeading && System.Text.Json.JsonSerializer.Serialize(after.Welcome) == System.Text.Json.JsonSerializer.Serialize(homeBefore.Welcome),
            "Statistics writes preserve all five previous CMS slices.");
        Check(after.Members.SequenceEqual(homeBefore.Members) && after.Projects.SequenceEqual(homeBefore.Projects), "Canonical cards stay unchanged.");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var html = await anonymous.GetStringAsync("/");
            Check(html.Contains("STAT 1") && html.Contains("data-count-value=\"725\"") && html.Contains("aria-label=\"725\""), "Anonymous Home and refresh render DB values for CountUp and accessible text.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeStatisticsInitializer.InitializeAsync(db);
            await HomeStatisticsInitializer.InitializeAsync(db);
            var rows = await db.HomeStatistics.OrderBy(x => x.DisplayOrder).ToListAsync();
            Check(rows.Count == 4 && rows.Select(x => x.Id).SequenceEqual(new[] { 1, 2, 3, 4 }), "Exactly four fixed identities in approved order.");
            Check(rows[0].Label == "STAT 1", "Repeated initialization preserves Admin edits.");
            Check(rows.Select(x => x.UpdatedAtUtc).Distinct().Count() == 1 && rows[0].UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5), "Collection save shares a recent UTC timestamp.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var client = restarted.NewClient();
            Check((await client.GetStringAsync("/")).Contains("data-count-value=\"725\""), "Restart preserves saved statistics.");
            Check((await Login(client, AuthFactory.Email, app.Password)).StatusCode == HttpStatusCode.Redirect, "Restart preserves Admin credentials.");
        }
        foreach (var index in Enumerable.Range(0, 4))
        {
            foreach (var label in new[] { "", "   ", new string('x', 33) })
            {
                var invalid = HomeStatisticsEditModel.FromContent(initial);
                invalid.Items[index].Label = label;
                await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Each label rejects blank/overlength values.");
            }
            foreach (var value in new int?[] { null, -1, 10000, int.MaxValue })
            {
                var invalid = HomeStatisticsEditModel.FromContent(initial);
                invalid.Items[index].Value = value;
                await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Each number rejects missing/out-of-range values.");
            }
        }
        foreach (var mutation in new Action<HomeStatisticsEditModel>[] {
            model => model.Items.RemoveAt(3),
            model => model.Items.Add(new() { Id = 5, Label = "Extra", Value = 1 }),
            model => model.Items.Reverse(),
            model => model.Items[1] = model.Items[0],
            model => model.Items[0] = new() { Id = 9, Label = "Unknown", Value = 1 } })
        {
            var invalid = HomeStatisticsEditModel.FromContent(initial);
            mutation(invalid);
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Add/delete/reorder/duplicate/unknown identities must be rejected.");
        }
        Check(Same(await service.GetAsync(), saved), "All rejected writes leave the whole collection unchanged.");
        // A DB failure on the second row must roll back the first row too.
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER TestStatisticsFailure BEFORE UPDATE ON HomeStatistics WHEN NEW.Id = 2 BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await ExpectAsync<DbUpdateException>(() => service.UpdateAsync(HomeStatisticsEditModel.FromContent(initial)), "Database failure propagates to editor.");
        Check(Same(await service.GetAsync(), saved), "Failed multi-row save rolls back the whole collection.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TestStatisticsFailure;");

        auth.User = new ClaimsPrincipal(new ClaimsIdentity());
        await ExpectAsync<UnauthorizedAccessException>(() => service.GetForEditAsync(), "Anonymous direct editor read denied.");
        await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Anonymous direct save denied.");
        auth.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "viewer")], "test"));
        await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Non-Admin direct save denied.");
        auth.User = principal;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            await users.RemoveFromRoleAsync(admin, "Admin");
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Current database role is required.");
            await users.AddToRoleAsync(admin, "Admin");
            await users.UpdateSecurityStampAsync(admin);
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(edit), "Revoked stamp cannot save.");
            auth.User = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
        }
        var unavailable = new HomeStatisticsContentService(new UnavailableFactory(), auth, options, NullLogger<HomeStatisticsContentService>.Instance);
        Check(Same(await unavailable.GetAsync(), initial), "Unavailable DB uses approved defaults without writes.");
        await ExpectAsync<SqliteException>(() => unavailable.GetForEditAsync(), "Editor read failure is not silent fallback.");
        // Only this suite's disposable database: verify partial and empty initialization policies.
        await using (var db = await factory.CreateDbContextAsync())
            await db.HomeStatistics.Where(x => x.Id == 4).ExecuteDeleteAsync();
        Check(Same(await service.GetAsync(), initial), "Partial collection falls back as a whole.");
        await ExpectAsync<ValidationException>(() => service.GetForEditAsync(), "Partial collection cannot be edited silently.");
        await ExpectAsync<ValidationException>(() => service.UpdateAsync(edit), "Save cannot insert or repair missing rows.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeStatisticsInitializer.InitializeAsync(db);
            Check(await db.HomeStatistics.CountAsync() == 3, "Nonempty partial collection is never overwritten or reseeded.");
            await db.HomeStatistics.ExecuteDeleteAsync();
        }
        Check(Same(await service.GetAsync(), initial), "Empty collection uses approved fallback.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeStatistics.CountAsync() == 0, "Public fallback never persists rows.");
            await HomeStatisticsInitializer.InitializeAsync(db);
            Check(await db.HomeStatistics.CountAsync() == 4, "Controlled initialization restores only an empty collection.");
        }
        foreach (var value in new[] { 0, 9999 })
        {
            var valid = HomeStatisticsEditModel.FromContent(initial);
            valid.Items[0].Value = value;
            await service.UpdateAsync(valid);
            Check((await service.GetAsync())[0].Value == value, "Numeric boundaries are accepted.");
        }
        var encoded = HomeStatisticsEditModel.FromContent(initial);
        encoded.Items[0].Label = "<script>alert(1)</script>";
        await service.UpdateAsync(encoded);
        var encodedHtml = await anonymous.GetStringAsync("/");
        Check(encodedHtml.Contains("&lt;script&gt;") && !encodedHtml.Contains(encoded.Items[0].Label), "Labels are Razor-encoded text, not HTML.");
        await service.UpdateAsync(HomeStatisticsEditModel.FromContent(initial));
        await VerifyUpgradeAsync();
    }

    private static async Task VerifyUpgradeAsync()
    {
        // Upgrade the previous schema, preserving Identity and all five existing edited Home slices.
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
        await migrator.MigrateAsync("20260922172549_AddHomeTeamSectionSettings");
        await HomeHeroInitializer.InitializeAsync(db);
        await HomeWelcomeInitializer.InitializeAsync(db);
        await HomeServicesSectionInitializer.InitializeAsync(db);
        await HomeProjectsSectionInitializer.InitializeAsync(db);
        await HomeTeamSectionInitializer.InitializeAsync(db);
        (await db.HomeTeamSectionSettings.SingleAsync()).Title = "Existing Team edit";
        (await db.HomeWelcomeSettings.SingleAsync()).Title = "Existing Welcome edit";
        (await db.HomeServicesSectionSettings.SingleAsync()).Title = "Existing Services edit";
        (await db.HomeProjectsSectionSettings.SingleAsync()).Title = "Existing Projects edit";
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
        await HomeStatisticsInitializer.InitializeAsync(db);
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync()).PasswordHash == originalHash && await db.UserRoles.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "Statistics upgrade must preserve existing Identity account/hash/role mapping.");
        Check((await db.HomeHeroSettings.SingleAsync()).OpeningLine == "Existing Hero edit", "Statistics migration must preserve Hero edits.");
        Check((await db.HomeWelcomeSettings.SingleAsync()).Title == "Existing Welcome edit" && (await db.HomeServicesSectionSettings.SingleAsync()).Title == "Existing Services edit" && (await db.HomeProjectsSectionSettings.SingleAsync()).Title == "Existing Projects edit", "Statistics migration preserves existing Home settings.");
        Check((await db.HomeTeamSectionSettings.SingleAsync()).Title == "Existing Team edit", "Statistics migration preserves Team edits.");
        Check(await db.HomeStatistics.CountAsync() == 4 && !db.Database.HasPendingModelChanges(), "Additive migration creates four statistics without model drift.");
    }

    private static bool Same(IEnumerable<Statistic> left, IEnumerable<Statistic> right) => left.SequenceEqual(right);

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
