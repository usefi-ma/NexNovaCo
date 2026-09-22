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

internal static class PartnerInitializationChecks
{
    public static async Task RunAsync()
    {
        // Only AuthFactory's disposable database; never the developer's normal database.
        await using var app = new AuthFactory(NewPassword());
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var initial = await ReadAsync(factory);
        Check(initial.Length == 6 && initial.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Fresh DB seeds approved count/order.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.PartnerInitializationStates.CountAsync() == 1, "Fresh seed records exactly one initialization marker.");
        await CheckRestartAsync(app, initial);

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>()
            .CreateUserPrincipalAsync((await users.FindByEmailAsync(AuthFactory.Email))!);
        var service = new PartnerContentService(factory, new TestAuth(principal),
            app.Services.GetRequiredService<IOptions<IdentityOptions>>(), NullLogger<PartnerContentService>.Instance);

        await service.DeleteAsync(initial[0].Id);
        var one = await ReadAsync(factory);
        Check(one.Length == 5 && one[0].Id == initial[1].Id, "Admin delete-one removes only its intended record.");
        await CheckRestartAsync(app, one);
        foreach (var row in one) await service.DeleteAsync(row.Id);
        await CheckRestartAsync(app, []);
        await CheckRestartAsync(app, []); // Repeated restarts must also preserve intentional emptiness.
        Check((await service.GetAsync()).Count == 0, "Initialized empty public read does not use approved fallback.");

        var newId = await service.CreateAsync(new PartnerEditModel
        {
            Name = "New partner after intentional empty", Description = "A new partner.", ImagePath = PartnerLogoAssets.Paths[0]
        });
        var added = await ReadAsync(factory);
        Check(added.Length == 1 && added[0].Id == newId && added[0].DisplayOrder == 1, "Add after empty creates only the Admin record.");
        await CheckRestartAsync(app, added);

        await CheckUpgradeAsync();
        await CheckAtomicInitializationAsync();
    }

    private static async Task CheckRestartAsync(AuthFactory original, PartnerEntity[] expected)
    {
        await using var restarted = new AuthFactory(NewPassword(), databasePath: original.DatabasePath);
        using var client = restarted.NewClient(); // Starts a new application host and runs real migrations/initializers.
        var factory = restarted.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        Check(JsonSerializer.Serialize(await ReadAsync(factory)) == JsonSerializer.Serialize(expected),
            "Application restart must preserve collection membership/content/order/IDs/timestamps exactly.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.PartnerInitializationStates.CountAsync() == 1, "Marker survives all restarts/deletions without duplication.");
        foreach (var route in new[] { "/", "/about" })
        {
            var response = await client.GetAsync(route);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Check(response.StatusCode == HttpStatusCode.OK, "Public page remains healthy after restart, including zero records.");
            Check(html.Contains("data-carousel-kind=\"partners\"") == (expected.Length > 0), "Empty public collection never renders an Owl partner root.");
            foreach (var row in expected)
                Check(html.Contains(row.Name) && html.Contains(row.Description), "Public restart preserves all approved/new quote text and paragraph content.");
        }
        await using var scope = restarted.Services.CreateAsyncScope();
        var expectedContent = JsonSerializer.Serialize(expected.Select(x => x.ToContent()));
        Check(JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync()).Partners) == expectedContent &&
            JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IAboutContentService>().GetAsync()).Partners) == expectedContent,
            "Home and About read the same exact persisted collection after restart.");
        Check((await Login(client, AuthFactory.Email, original.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,
            "Marker initialization/restart preserves existing Admin credentials.");
    }

    private static async Task<PartnerEntity[]> ReadAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Partners.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync();
    }

    private static async Task CheckUpgradeAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260922212453_AddTestimonialInitializationState");
        await HomePartnersSectionInitializer.InitializeAsync(db);
        await TestimonialInitializer.InitializeAsync(db);
        (await db.HomePartnersSectionSettings.SingleAsync()).Title = "Existing Home intro";
        (await db.Testimonials.OrderBy(x => x.Id).FirstAsync()).Quote = "Existing testimonial edit";
        await db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await db.Testimonials.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        await migrator.MigrateAsync();
        await PartnerInitializer.InitializeAsync(db);
        Check((await db.HomePartnersSectionSettings.SingleAsync()).Title == "Existing Home intro" &&
            JsonSerializer.Serialize(await db.Testimonials.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()) == before &&
            await db.TestimonialInitializationStates.CountAsync() == 1,
            "Additive Partners migration preserves existing Home intro, Testimonials edits and initialization state.");
        Check(await db.Partners.CountAsync() == 6 && await db.PartnerInitializationStates.CountAsync() == 1 &&
            !db.Database.HasPendingModelChanges(), "Upgrade seeds only new Partners and has no model drift.");
    }

    private static async Task CheckAtomicInitializationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailPartnerMarker BEFORE INSERT ON PartnerInitializationState BEGIN SELECT RAISE(ABORT, 'isolated test failure'); END;");
        var failed = false;
        try { await PartnerInitializer.InitializeAsync(db); }
        catch (DbUpdateException) { failed = true; }
        db.ChangeTracker.Clear();
        Check(failed && await db.Partners.CountAsync() == 0 && !await db.PartnerInitializationStates.AnyAsync(),
            "Failed initialization rolls back both default rows and marker together.");
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailPartnerMarker;");
        await PartnerInitializer.InitializeAsync(db);
        Check(await db.Partners.CountAsync() == 6 && await db.PartnerInitializationStates.CountAsync() == 1,
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
