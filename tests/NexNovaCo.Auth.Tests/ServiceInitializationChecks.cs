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

internal static class ServiceInitializationChecks
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
            Check(await db.ServiceInitializationStates.CountAsync() == 1, "Fresh seed records exactly one initialization marker.");
        await CheckRestartAsync(app, initial, initial.Take(5).Select(x => x.Id).ToArray());

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>()
            .CreateUserPrincipalAsync((await users.FindByEmailAsync(AuthFactory.Email))!);
        var service = new ServiceContentService(factory, new TestAuth(principal),
            app.Services.GetRequiredService<IOptions<IdentityOptions>>(), NullLogger<ServiceContentService>.Instance);

        var reordered = initial.Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(reordered);
        var selected = new[] { initial[2].Id, initial[0].Id };
        await service.SaveHomeFeaturedAsync(selected);
        var edit = initial[2].ToEditModel(); edit.Name = "Persisted edited Service";
        await service.UpdateAsync(initial[2].Id, edit);
        await CheckRestartAsync(app, await ReadAsync(factory), selected);
        await service.SaveHomeFeaturedAsync([]);
        await CheckRestartAsync(app, await ReadAsync(factory), []);
        await CheckRestartAsync(app, await ReadAsync(factory), []);
        await service.SaveHomeFeaturedAsync(initial.Take(5).Select(x => x.Id).ToArray());
        await service.DeleteAsync(initial[0].Id);
        var one = await ReadAsync(factory);
        Check(one.Length == 5 && one.All(x => x.Id != initial[0].Id), "Admin delete-one removes only its intended record.");
        await CheckRestartAsync(app, one, initial.Skip(1).Take(4).Select(x => x.Id).ToArray());
        foreach (var row in one) await service.DeleteAsync(row.Id);
        await CheckRestartAsync(app, [], []);
        await CheckRestartAsync(app, [], []); // Repeated restarts must also preserve intentional emptiness.
        Check((await service.GetAsync()).Count == 0, "Initialized empty public read does not use approved fallback.");

        var newId = await service.CreateAsync(new ServiceEditModel
        {
            Name = "New service after intentional empty", Tagline = "New tagline", Description = "A new service.", IconPath = ServiceIconAssets.Paths[0]
        });
        var added = await ReadAsync(factory);
        Check(added.Length == 1 && added[0].Id == newId && added[0].DisplayOrder == 1, "Add after empty creates only the Admin record.");
        await CheckRestartAsync(app, added, []);
        await service.SaveHomeFeaturedAsync([newId]);
        await CheckRestartAsync(app, added, [newId]);

        await CheckUpgradeAsync();
        await CheckAtomicInitializationAsync();
    }

    private static async Task CheckRestartAsync(AuthFactory original, ServiceEntity[] expected, int[] featuredIds)
    {
        await using var restarted = new AuthFactory(NewPassword(), databasePath: original.DatabasePath);
        using var client = restarted.NewClient(); // Starts a new application host and runs real migrations/initializers.
        var factory = restarted.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        Check(JsonSerializer.Serialize(await ReadAsync(factory)) == JsonSerializer.Serialize(expected),
            "Application restart must preserve collection membership/content/order/IDs/timestamps exactly.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.ServiceInitializationStates.CountAsync() == 1, "Marker survives all restarts/deletions without duplication.");
        foreach (var route in new[] { "/", "/services" })
        {
            var response = await client.GetAsync(route);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Check(response.StatusCode == HttpStatusCode.OK, "Public page remains healthy after restart, including zero records.");
            Check(html.Contains("class=\"service_box\"") == (route == "/" ? featuredIds.Length > 0 : expected.Length > 0), "Only the correct nonempty public collection renders Service cards.");
            foreach (var row in (route == "/" ? expected.Where(x => featuredIds.Contains(x.Id)) : expected))
                Check(html.Contains(row.Name) && html.Contains(row.Description), "Public restart preserves all approved/new quote text and paragraph content.");
        }
        await using var scope = restarted.Services.CreateAsyncScope();
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeFeaturedServices.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.ServiceId).ToArrayAsync()).SequenceEqual(featuredIds),
                "Restart preserves exact relation membership/order, including intentionally empty selection.");
        var expectedContent = JsonSerializer.Serialize(expected.Select(x => x.ToContent()));
        Check(JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync()).Services) == JsonSerializer.Serialize(featuredIds.Select(id => expected.Single(x => x.Id == id).ToContent())) &&
            JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IServicesContentService>().GetAsync()).Services) == expectedContent,
            "Full catalog and independently selected/order Home subset persist across restart.");
        Check((await Login(client, AuthFactory.Email, original.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,
            "Marker initialization/restart preserves existing Admin credentials.");
    }

    private static async Task<ServiceEntity[]> ReadAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Services.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync();
    }

    private static async Task CheckUpgradeAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260922215318_AddSharedPartners");
        await HistoricalHomeFixture.InitializeAsync(db);
        await HomeServicesSectionInitializer.InitializeAsync(db); await HomeProjectsSectionInitializer.InitializeAsync(db);
        await HomeTeamSectionInitializer.InitializeAsync(db); await HomeStatisticsInitializer.InitializeAsync(db);
        await HomePartnersSectionInitializer.InitializeAsync(db); await HomeTestimonialsSectionInitializer.InitializeAsync(db);
        await PartnerInitializer.InitializeAsync(db); await TestimonialInitializer.InitializeAsync(db);
        (await db.HomeServicesSectionSettings.SingleAsync()).Title = "Existing Services intro";
        (await db.Partners.FirstAsync()).Name = "Existing Partner edit";
        (await db.Testimonials.FirstAsync()).Quote = "Existing Testimonial edit";
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "upgrade@example.invalid" };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, NewPassword());
        var role = new IdentityRole("Admin") { Id = Guid.NewGuid().ToString() };
        db.Users.Add(user); db.Roles.Add(role); db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        async Task<string> Previous() => JsonSerializer.Serialize(new
        {
            Home = await HistoricalHomeFixture.SnapshotAsync(db),
            Services = await db.HomeServicesSectionSettings.AsNoTracking().ToArrayAsync(), Projects = await db.HomeProjectsSectionSettings.AsNoTracking().ToArrayAsync(),
            Team = await db.HomeTeamSectionSettings.AsNoTracking().ToArrayAsync(), Stats = await db.HomeStatistics.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            PartnersIntro = await db.HomePartnersSectionSettings.AsNoTracking().ToArrayAsync(), TestimonialsIntro = await db.HomeTestimonialsSectionSettings.AsNoTracking().ToArrayAsync(),
            Partners = await db.Partners.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Testimonials = await db.Testimonials.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            Users = await db.Users.AsNoTracking().ToArrayAsync(), Roles = await db.Roles.AsNoTracking().ToArrayAsync(), Membership = await db.UserRoles.AsNoTracking().ToArrayAsync()
        });
        var before = await Previous();
        await migrator.MigrateAsync(); await ServiceInitializer.InitializeAsync(db);
        Check(await Previous() == before, "Additive migration leaves Identity/hash/roles, all eight Home CMS slices, Testimonials and Partners untouched.");
        Check(await db.TestimonialInitializationStates.CountAsync() == 1 && await db.PartnerInitializationStates.CountAsync() == 1, "Previous initialization markers intact.");
        Check(await db.Services.CountAsync() == 6 && await db.HomeFeaturedServices.CountAsync() == 5 && !db.Database.HasPendingModelChanges(), "New Service tables initialized without model drift.");
        db.HomeFeaturedServices.Add(new HomeFeaturedService { ServiceId = int.MaxValue, DisplayOrder = 1 });
        var rejected = false;
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { rejected = true; }
        Check(rejected, "Foreign key rejects nonexistent Service selection.");
    }

    private static async Task CheckAtomicInitializationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailServiceMarker BEFORE INSERT ON ServiceInitializationState BEGIN SELECT RAISE(ABORT, 'isolated test failure'); END;");
        var failed = false;
        try { await ServiceInitializer.InitializeAsync(db); }
        catch (DbUpdateException) { failed = true; }
        db.ChangeTracker.Clear();
        Check(failed && await db.Services.CountAsync() == 0 && await db.HomeFeaturedServices.CountAsync() == 0 && !await db.ServiceInitializationStates.AnyAsync(),
            "Failed initialization rolls back both default rows and marker together.");
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailServiceMarker;");
        await ServiceInitializer.InitializeAsync(db);
        Check(await db.Services.CountAsync() == 6 && await db.HomeFeaturedServices.CountAsync() == 5 && await db.ServiceInitializationStates.CountAsync() == 1,
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
