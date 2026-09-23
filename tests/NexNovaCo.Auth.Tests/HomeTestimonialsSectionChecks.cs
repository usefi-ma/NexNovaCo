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

internal static class HomeTestimonialsSectionChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        var denied = await anonymous.GetAsync("/dashboard/content/home/testimonials");
        Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous TestimonialsSection editor must challenge.");
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        var editor = await adminClient.GetAsync("/dashboard/content/home/testimonials");
        var editorHtml = await editor.Content.ReadAsStringAsync();
        Check(editor.StatusCode == HttpStatusCode.OK && editorHtml.Contains("NexNovaCo") && !editorHtml.Contains("Opening line"), "Admin editor must load saved content.");
        Check(editorHtml.Contains("mud-input") && editorHtml.Contains("\"type\":\"server\""), "TestimonialsSection editor must use interactive Mud inputs.");
        Check((await adminClient.GetAsync("/dashboard/content/home/testimonials")).StatusCode == HttpStatusCode.OK, "Testimonials editor must support direct refresh.");
        Check(editorHtml.Contains("href=\"/dashboard/content/home/testimonials\"") && editorHtml.Contains("Home / Testimonials"), "Testimonials editor must have its own sidebar link and page context.");
        Check(typeof(HomeTestimonialsSectionEditModel).GetProperties().Select(x => x.Name).Order().SequenceEqual(new[] { "Description", "Title" }), "Only the two actual intro fields may be editable.");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        ClaimsPrincipal principal;
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "testimonials-viewer@example.invalid", Email = "testimonials-viewer@example.invalid" }, viewerPassword)).Succeeded, "CMS non-Admin test setup failed.");
        }
        using var viewerClient = app.NewClient();
        await Login(viewerClient, "testimonials-viewer@example.invalid", viewerPassword);
        var viewerEditor = await viewerClient.GetAsync("/dashboard/content/home/testimonials");
        Check(viewerEditor.StatusCode == HttpStatusCode.Redirect && viewerEditor.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin editor access must be denied.");

        var auth = new TestAuthenticationStateProvider(principal);
        var service = new HomeTestimonialsSectionContentService(factory, auth, options, NullLogger<HomeTestimonialsSectionContentService>.Instance);
        var initial = await service.GetAsync();
        Check(Same(initial, HomeTestimonialsSectionDefaults.Content), "Initial CMS copy must exactly match approved defaults, including description.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeTestimonialsSectionSettings.CountAsync() == 1, "Startup must create exactly one TestimonialsSection.");
            Check((await db.HomeTestimonialsSectionSettings.SingleAsync()).Id == 1, "TestimonialsSection must use fixed Id 1.");
        }
        HomeHeroContent heroBefore;
        string welcomeBefore;
        SectionHeading servicesBefore;
        await using (var db = await factory.CreateDbContextAsync()) servicesBefore = (await db.HomeServicesSectionSettings.SingleAsync()).ToContent();
        await using var projectsScope = app.Services.CreateAsyncScope();
        var projectsService = projectsScope.ServiceProvider.GetRequiredService<IProjectsContentService>();
        var testimonialsPageBefore = System.Text.Json.JsonSerializer.Serialize(await projectsService.GetAsync());
        HomeContent homeBefore;
        await using (var scope = app.Services.CreateAsyncScope())
            homeBefore = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
        await using (var db = await factory.CreateDbContextAsync()) welcomeBefore = System.Text.Json.JsonSerializer.Serialize((await db.HomeWelcomeSettings.SingleAsync()).ToContent());
        await using (var db = await factory.CreateDbContextAsync()) heroBefore = (await db.HomeHeroSettings.SingleAsync()).ToContent();
        var projectsBefore = await projectsService.GetAsync();
        Check(SameTestimonials(homeBefore.Testimonials, projectsBefore.Testimonials) && homeBefore.Testimonials.Count == 2, "Home and Projects must read the same ordered two-testimonial DB collection.");
        Check(homeBefore.TestimonialBrand == projectsBefore.TestimonialBrand, "Initial Home/Projects introductions match approved copy.");
        var edit = await service.GetForEditAsync();
        edit.Title = "CMS persistence verified";
        edit.Description = "Edited Home Testimonials introduction for isolated persistence verification.";
        await service.UpdateAsync(edit);
        Check((await service.GetAsync()).Title == edit.Title, "Update must persist into a new context.");
        Check(System.Text.Json.JsonSerializer.Serialize(await projectsService.GetAsync()) == testimonialsPageBefore, "Edited Home intro must leave public Projects content unchanged.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var updated = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(updated.TestimonialBrand == edit.ToContent() && SameTestimonials(updated.Testimonials, homeBefore.Testimonials), "Only the intro may change; every canonical card must stay identical.");
        }
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeHeroSettings.SingleAsync()).ToContent() == heroBefore, "TestimonialsSection save must not alter Hero.");
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var publicHtml = await anonymous.GetStringAsync("/");
            Check(publicHtml.Contains(edit.Title) && publicHtml.Contains(edit.Description), "Anonymous Home/refresh must read edited TestimonialsSection from SQLite.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await HomeTestimonialsSectionInitializer.InitializeAsync(db);
            await HomeTestimonialsSectionInitializer.InitializeAsync(db);
            Check(await db.HomeTestimonialsSectionSettings.CountAsync() == 1 && (await db.HomeTestimonialsSectionSettings.SingleAsync()).Title == edit.Title, "Repeated initialization must not overwrite edits or duplicate singleton.");
            Check((await db.HomeTestimonialsSectionSettings.SingleAsync()).UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5), "Save must set a recent UTC timestamp.");
            db.HomeTestimonialsSectionSettings.Add(new HomeTestimonialsSectionSettings { Id = 2 });
            await ExpectAsync<DbUpdateException>(() => db.SaveChangesAsync(), "Database must reject a second singleton id.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var restartClient = restarted.NewClient();
            Check((await restartClient.GetStringAsync("/")).Contains(edit.Title), "New application host/startup must preserve saved copy.");
            Check((await Login(restartClient, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Restart must preserve existing Admin password.");
        }

        foreach (var property in typeof(HomeTestimonialsSectionEditModel).GetProperties())
        {
            var invalid = HomeTestimonialsSectionEditModel.FromContent(initial);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every TestimonialsSection field must reject whitespace.");
            property.SetValue(invalid, new string('x', property.GetCustomAttributes(typeof(StringLengthAttribute), false).Cast<StringLengthAttribute>().Single().MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(invalid), "Every TestimonialsSection field must enforce its length boundary.");
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
            await db.HomeTestimonialsSectionSettings.ExecuteDeleteAsync(); // Only this suite's isolated database.
        Check(Same(await service.GetAsync(), initial), "Missing record must fall back to approved copy.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(), "Editor must report missing data, not silently use defaults.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.HomeTestimonialsSectionSettings.CountAsync() == 0, "Public fallback must not persist a record.");
            await HomeTestimonialsSectionInitializer.InitializeAsync(db);
            Check(await db.HomeTestimonialsSectionSettings.CountAsync() == 1, "Controlled initialization must repair a missing record.");
        }
        var unavailable = new HomeTestimonialsSectionContentService(new UnavailableFactory(), auth, options, NullLogger<HomeTestimonialsSectionContentService>.Instance);
        Check(Same(await unavailable.GetAsync(), initial), "Unavailable database must fall back safely.");
        await ExpectAsync<SqliteException>(() => unavailable.UpdateAsync(edit), "Write failures must propagate for safe editor feedback.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.HomeTestimonialsSectionSettings.ExecuteUpdateAsync(set => set.SetProperty(x => x.Title, ""));
        Check(Same(await service.GetAsync(), initial), "Invalid stored content must use approved defaults.");
        // Razor encodes content; there is no MarkupString/HTML editor.
        var encoded = HomeTestimonialsSectionEditModel.FromContent(initial);
        encoded.Title = "<script>alert(1)</script>";
        await service.UpdateAsync(encoded);
        var encodedHtml = await anonymous.GetStringAsync("/");
        Check(encodedHtml.Contains("&lt;script&gt;") && !encodedHtml.Contains(encoded.Title), "TestimonialsSection text must remain HTML-encoded.");
        await service.UpdateAsync(HomeTestimonialsSectionEditModel.FromContent(initial));

        Check(System.Text.Json.JsonSerializer.Serialize(await projectsService.GetAsync()) == testimonialsPageBefore, "Home intro editing must not affect public Projects page content.");
        Check((await anonymous.GetAsync("/projects")).StatusCode == HttpStatusCode.OK, "Projects page must remain anonymous.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(System.Text.Json.JsonSerializer.Serialize((await db.HomeWelcomeSettings.SingleAsync()).ToContent()) == welcomeBefore, "Testimonials intro writes must not alter Welcome.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var homeAfter = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            Check(SameTestimonials(homeBefore.Testimonials, homeAfter.Testimonials) && homeAfter.Testimonials.Count == 2, "Home testimonial cards, order, copy, quotes and authors must remain canonical.");
        }
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeServicesSectionSettings.SingleAsync()).ToContent() == servicesBefore, "Testimonials intro must not modify saved Services settings.");
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var homeAfter = await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync();
            var projectsAfter = await projectsService.GetAsync();
            Check(SameTestimonials(homeAfter.Testimonials, projectsAfter.Testimonials), "Home/Projects must still read one canonical DB collection after saves.");
            Check(homeAfter.ProjectsHeading == homeBefore.ProjectsHeading && homeAfter.Team == homeBefore.Team &&
                homeAfter.Statistics.SequenceEqual(homeBefore.Statistics) && homeAfter.PartnersHeading == homeBefore.PartnersHeading, "Testimonials edits preserve Projects, Team and Statistics settings.");
        }
        await VerifyUpgradeAsync();
    }

    private static async Task VerifyUpgradeAsync()
    {
        // Upgrade the previous Partners schema, preserving Identity and all seven existing Home slices.
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
        await migrator.MigrateAsync("20260922194221_AddHomePartnersSectionSettings");
        await HistoricalHomeFixture.InitializeAsync(db);
        await HomeServicesSectionInitializer.InitializeAsync(db);
        await HomeProjectsSectionInitializer.InitializeAsync(db);
        await HomeTeamSectionInitializer.InitializeAsync(db);
        await HomeStatisticsInitializer.InitializeAsync(db);
        await HomePartnersSectionInitializer.InitializeAsync(db);
        (await db.HomePartnersSectionSettings.SingleAsync()).Title = "Existing Partners edit";
        (await db.HomeProjectsSectionSettings.SingleAsync()).Title = "Existing Projects edit";
        (await db.HomeTeamSectionSettings.SingleAsync()).Title = "Existing Team edit";
        (await db.HomeStatistics.SingleAsync(x => x.Id == 1)).Value = 875;
        var servicesIntro = await db.HomeServicesSectionSettings.SingleAsync();
        servicesIntro.Title = "Existing Services edit";
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
        await HomeTestimonialsSectionInitializer.InitializeAsync(db);
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync()).PasswordHash == originalHash && await db.UserRoles.CountAsync() == 1 && await db.Roles.CountAsync() == 1, "TestimonialsSection upgrade must preserve existing Identity account/hash/role mapping.");
        Check((await db.HomeHeroSettings.SingleAsync()).OpeningLine == "Existing Hero edit", "TestimonialsSection migration must preserve Hero edits.");
        Check((await db.HomeWelcomeSettings.SingleAsync()).Title == "Existing Welcome edit", "Testimonials migration must preserve Welcome edits.");
        Check((await db.HomeServicesSectionSettings.SingleAsync()).Title == "Existing Services edit", "Testimonials migration must preserve Services intro edits.");
        Check((await db.HomeProjectsSectionSettings.SingleAsync()).Title == "Existing Projects edit" &&
            (await db.HomeTeamSectionSettings.SingleAsync()).Title == "Existing Team edit" &&
            (await db.HomeStatistics.SingleAsync(x => x.Id == 1)).Value == 875, "Testimonials migration preserves the later three Home slices.");
        Check((await db.HomePartnersSectionSettings.SingleAsync()).Title == "Existing Partners edit", "Testimonials migration preserves Partners settings.");
        Check(await db.HomeTestimonialsSectionSettings.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Additive migration must create TestimonialsSection table with no model drift.");
    }

    private static bool Same(SectionHeading left, SectionHeading right) => left == right;
    private static bool SameTestimonials(IReadOnlyList<Testimonial> left, IReadOnlyList<Testimonial> right) =>
        System.Text.Json.JsonSerializer.Serialize(left) == System.Text.Json.JsonSerializer.Serialize(right);

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
