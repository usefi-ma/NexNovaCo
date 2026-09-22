using System.Net;
using System.Security.Claims;
using System.Text.Json;
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

internal static class TestimonialInitializationChecks
{
    public static async Task RunAsync()
    {
        // Only AuthFactory's disposable database; never the developer's normal database.
        await using var app = new AuthFactory(NewPassword());
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var initial = await ReadAsync(factory);
        Check(initial.Length == 2 && initial.Select(x => x.DisplayOrder).SequenceEqual([1, 2]) &&
            initial[0].Attribution == "Olivia Carter, COO at Alpha Co" &&
            initial[1].Attribution == "Daniel Kim, Marketing Director at Tech Co" &&
            initial.All(x => x.ToContent().Paragraphs.Count == 2), "Fresh DB seeds approved count/order/attribution/paragraph structure.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.TestimonialInitializationStates.CountAsync() == 1, "Fresh seed records exactly one initialization marker.");
        await CheckRestartAsync(app, initial);

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>()
            .CreateUserPrincipalAsync((await users.FindByEmailAsync(AuthFactory.Email))!);
        var service = new TestimonialContentService(factory, new TestAuth(principal),
            app.Services.GetRequiredService<IOptions<IdentityOptions>>(), NullLogger<TestimonialContentService>.Instance);

        await service.DeleteAsync(initial[0].Id);
        var one = await ReadAsync(factory);
        Check(one.Length == 1 && one[0].Id == initial[1].Id, "Admin delete-one removes only its intended record.");
        await CheckRestartAsync(app, one);
        await service.DeleteAsync(one[0].Id);
        await CheckRestartAsync(app, []);
        await CheckRestartAsync(app, []); // Repeated restarts must also preserve intentional emptiness.
        Check((await service.GetAsync()).Count == 0, "Initialized empty public read does not use approved fallback.");

        var newId = await service.CreateAsync(new TestimonialEditModel
        {
            Attribution = "New author after intentional empty", Quote = "A new quote.\n\nA second paragraph."
        });
        var added = await ReadAsync(factory);
        Check(added.Length == 1 && added[0].Id == newId && added[0].DisplayOrder == 1, "Add after empty creates only the Admin record.");
        await CheckRestartAsync(app, added);

        foreach (var scenario in new[] { "never-initialized", "all-deleted", "one-deleted", "custom-content" })
            await CheckLegacyUpgradeAsync(scenario);
        await CheckAtomicInitializationAsync();
    }

    private static async Task CheckRestartAsync(AuthFactory original, TestimonialEntity[] expected)
    {
        await using var restarted = new AuthFactory(NewPassword(), databasePath: original.DatabasePath);
        using var client = restarted.NewClient(); // Starts a new application host and runs real migrations/initializers.
        var factory = restarted.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        Check(JsonSerializer.Serialize(await ReadAsync(factory)) == JsonSerializer.Serialize(expected),
            "Application restart must preserve collection membership/content/order/IDs/timestamps exactly.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.TestimonialInitializationStates.CountAsync() == 1, "Marker survives all restarts/deletions without duplication.");
        foreach (var route in new[] { "/", "/projects" })
        {
            var response = await client.GetAsync(route);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Check(response.StatusCode == HttpStatusCode.OK, "Public page remains healthy after restart, including zero records.");
            Check(html.Contains("data-carousel-kind=\"testimonials\"") == (expected.Length > 0), "Empty public collection never renders an Owl testimonial root.");
            foreach (var row in expected)
                Check(html.Contains(row.Attribution) && row.ToContent().Paragraphs.All(html.Contains), "Public restart preserves all approved/new quote text and paragraph content.");
        }
        await using var scope = restarted.Services.CreateAsyncScope();
        var expectedContent = JsonSerializer.Serialize(expected.Select(x => x.ToContent()));
        Check(JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync()).Testimonials) == expectedContent &&
            JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IProjectsContentService>().GetAsync()).Testimonials) == expectedContent,
            "Home and Projects read the same exact persisted collection after restart.");
        Check((await Login(client, AuthFactory.Email, original.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,
            "Marker initialization/restart preserves existing Admin credentials.");
    }

    private static async Task<TestimonialEntity[]> ReadAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Testimonials.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync();
    }

    private static async Task CheckLegacyUpgradeAsync(string scenario)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260922203426_AddSharedTestimonials");
        if (scenario != "never-initialized")
        {
            var first = new TestimonialEntity { DisplayOrder = 1 };
            first.SetContent(new TestimonialEditModel { Attribution = "Existing edited author", Quote = "Existing edited quote." });
            var second = new TestimonialEntity { DisplayOrder = 2 };
            second.SetContent(new TestimonialEditModel { Attribution = "Another existing author", Quote = "Another existing quote." });
            db.Testimonials.AddRange(first, second);
            await db.SaveChangesAsync();
            if (scenario == "all-deleted") await db.Testimonials.ExecuteDeleteAsync();
            if (scenario == "one-deleted") await db.Testimonials.Where(x => x.Id == first.Id).ExecuteDeleteAsync();
            db.ChangeTracker.Clear();
        }
        var before = JsonSerializer.Serialize(await db.Testimonials.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        await migrator.MigrateAsync();
        Check(await db.TestimonialInitializationStates.AnyAsync() == (scenario != "never-initialized"),
            "Upgrade recognizes historical initialization, even when every legacy row was deleted.");
        await TestimonialInitializer.InitializeAsync(db);
        await TestimonialInitializer.InitializeAsync(db);
        Check(await db.TestimonialInitializationStates.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Upgrade/seed marker is idempotent and model matches migration.");
        if (scenario == "never-initialized")
            Check(await db.Testimonials.CountAsync() == 2, "A migrated but never initialized legacy DB still receives defaults.");
        else
            Check(JsonSerializer.Serialize(await db.Testimonials.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()) == before,
                "Upgrade preserves deleted-one/deleted-all/custom legacy content without reseeding or overwriting.");
    }

    private static async Task CheckAtomicInitializationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailTestimonialMarker BEFORE INSERT ON TestimonialInitializationState BEGIN SELECT RAISE(ABORT, 'isolated test failure'); END;");
        var failed = false;
        try { await TestimonialInitializer.InitializeAsync(db); }
        catch (DbUpdateException) { failed = true; }
        db.ChangeTracker.Clear();
        Check(failed && await db.Testimonials.CountAsync() == 0 && !await db.TestimonialInitializationStates.AnyAsync(),
            "Failed initialization rolls back both default rows and marker together.");
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailTestimonialMarker;");
        await TestimonialInitializer.InitializeAsync(db);
        Check(await db.Testimonials.CountAsync() == 2 && await db.TestimonialInitializationStates.CountAsync() == 1,
            "A failed uncommitted initialization can be retried safely.");
    }

    private static ServiceProvider CreateProvider(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(connection));
        return services.BuildServiceProvider();
    }

    private sealed class TestAuth(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
    }
}
