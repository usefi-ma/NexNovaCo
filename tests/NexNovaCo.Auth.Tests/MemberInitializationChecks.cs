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

internal static class MemberInitializationChecks
{
    public static async Task RunAsync()
    {
        // Only AuthFactory's disposable database; never the developer's normal database.
        await using var app = new AuthFactory(NewPassword());
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var initial = await ReadAsync(factory);
        var approved = await app.Services.GetRequiredService<MemberCatalog>().GetDetailsAsync();
        Check(JsonSerializer.Serialize(initial.Select(x => x.ToDetail())) == JsonSerializer.Serialize(approved), "Fresh seed preserves every approved biography, skill and contact field in order.");
        Check(initial.Length == 6 && initial.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Fresh DB seeds approved count/order.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.MemberInitializationStates.CountAsync() == 1, "Fresh seed records exactly one initialization marker.");
        await CheckRestartAsync(app, initial, initial.Take(4).Select(x => x.Id).ToArray());

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>()
            .CreateUserPrincipalAsync((await users.FindByEmailAsync(AuthFactory.Email))!);
        var service = new MemberContentService(factory, new TestAuth(principal),
            app.Services.GetRequiredService<IOptions<IdentityOptions>>(), NullLogger<MemberContentService>.Instance, app.Services.GetRequiredService<MemberCatalog>());

        var reordered = initial.Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(reordered);
        var selected = new[] { initial[2].Id, initial[0].Id };
        await service.SaveHomeFeaturedAsync(selected);
        var edit = initial[2].ToEditModel(); edit.Name = "Persisted edited Member"; edit.Biography = "Persisted full detail"; edit.Role = "Updated role"; edit.Email = "edited@example.invalid"; edit.LinkedIn = ""; edit.Telegram = "https://t.me/test";
        edit.Skills.Add(new() { Text = "Persisted skill" });
        edit.Skills.RemoveAt(0); edit.Skills.Reverse();
        await service.UpdateAsync(initial[2].Id, edit);
        await CheckRestartAsync(app, await ReadAsync(factory), selected);
        await service.SaveHomeFeaturedAsync([]);
        await CheckRestartAsync(app, await ReadAsync(factory), []);
        await CheckRestartAsync(app, await ReadAsync(factory), []);
        await service.SaveHomeFeaturedAsync(initial.Take(4).Select(x => x.Id).ToArray());
        await service.DeleteAsync(initial[0].Id);
        var one = await ReadAsync(factory);
        Check(one.Length == 5 && one.All(x => x.Id != initial[0].Id), "Admin delete-one removes only its intended record.");
        await CheckRestartAsync(app, one, initial.Skip(1).Take(3).Select(x => x.Id).ToArray());
        foreach (var row in one) await service.DeleteAsync(row.Id);
        await CheckRestartAsync(app, [], []);
        await CheckRestartAsync(app, [], []); // Repeated restarts must also preserve intentional emptiness.
        Check((await service.GetAsync()).Count == 0, "Initialized empty public read does not use approved fallback.");

        var newId = await service.CreateAsync(new MemberEditModel
        {
            Slug = "new-after-empty", Biography = "Full new description", Name = "New member after intentional empty", Role = "New tagline", Introduction = "A new member.", ImagePath = MemberImageAssets.Paths[0]
        });
        var added = await ReadAsync(factory);
        Check(added.Length == 1 && added[0].Id == newId && added[0].DisplayOrder == 1, "Add after empty creates only the Admin record.");
        await CheckRestartAsync(app, added, []);
        await service.SaveHomeFeaturedAsync([newId]);
        await CheckRestartAsync(app, added, [newId]);

        await CheckUpgradeAsync(app.Services.GetRequiredService<MemberCatalog>(), app.Services.GetRequiredService<ProjectCatalog>());
        await CheckAtomicInitializationAsync(app.Services.GetRequiredService<MemberCatalog>());
    }

    private static async Task CheckRestartAsync(AuthFactory original, MemberEntity[] expected, int[] featuredIds)
    {
        await using var restarted = new AuthFactory(NewPassword(), databasePath: original.DatabasePath);
        using var client = restarted.NewClient(); // Starts a new application host and runs real migrations/initializers.
        var factory = restarted.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        Check(JsonSerializer.Serialize(await ReadAsync(factory)) == JsonSerializer.Serialize(expected),
            "Application restart must preserve collection membership/content/order/IDs/timestamps exactly.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.MemberInitializationStates.CountAsync() == 1, "Marker survives all restarts/deletions without duplication.");
        foreach (var route in new[] { "/", "/team" })
        {
            var response = await client.GetAsync(route);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Check(response.StatusCode == HttpStatusCode.OK, "Public page remains healthy after restart, including zero records.");
            Check(html.Contains("class=\"team_member_box\"") == (route == "/" ? featuredIds.Length > 0 : expected.Length > 0), "Only the correct nonempty public collection renders Member cards.");
            foreach (var row in (route == "/" ? expected.Where(x => featuredIds.Contains(x.Id)) : expected))
                Check(html.Contains(row.Name) && html.Contains(row.Introduction), "Public restart preserves all approved/member introduction.");
        }
        await using var scope = restarted.Services.CreateAsyncScope();
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.HomeFeaturedMembers.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.MemberId).ToArrayAsync()).SequenceEqual(featuredIds),
                "Restart preserves exact relation membership/order, including intentionally empty selection.");
        var catalog = scope.ServiceProvider.GetRequiredService<IMemberCatalog>();
        foreach (var row in expected)
        {
            Check(JsonSerializer.Serialize(await catalog.GetDetailAsync(row.Slug)) == JsonSerializer.Serialize(row.ToDetail()), "Detail and ordered children survive application restart.");
            var detailResponse = await client.GetAsync("/team/" + row.Slug);
            var detailHtml = WebUtility.HtmlDecode(await detailResponse.Content.ReadAsStringAsync());
            Check(detailResponse.IsSuccessStatusCode && detailHtml.Contains(row.Biography), "Restart serves the saved public detail.");
        }
        Check(await catalog.GetDetailAsync("not-a-real-member") is null &&
            (await client.GetAsync("/team/not-a-real-member")).StatusCode == HttpStatusCode.NotFound, "Unknown detail remains safe including empty catalog.");
        var expectedContent = JsonSerializer.Serialize(expected.Select(x => x.ToContent()));
        Check(JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<IHomeContentService>().GetAsync()).Members) == JsonSerializer.Serialize(featuredIds.Select(id => expected.Single(x => x.Id == id).ToContent())) &&
            JsonSerializer.Serialize((await scope.ServiceProvider.GetRequiredService<ITeamContentService>().GetAsync()).Members) == expectedContent,
            "Full catalog and independently selected/order Home subset persist across restart.");
        Check((await Login(client, AuthFactory.Email, original.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,
            "Marker initialization/restart preserves existing Admin credentials.");
    }

    private static async Task<MemberEntity[]> ReadAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Members.AsNoTracking().Include(x => x.Skills).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync();
    }

    private static async Task CheckUpgradeAsync(MemberCatalog defaults, ProjectCatalog projectDefaults)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260922230323_AddSharedProjectsAndHomeFeatured");
        await HomeHeroInitializer.InitializeAsync(db); await HomeWelcomeInitializer.InitializeAsync(db);
        await HomeServicesSectionInitializer.InitializeAsync(db); await HomeProjectsSectionInitializer.InitializeAsync(db);
        await HomeTeamSectionInitializer.InitializeAsync(db); await HomeStatisticsInitializer.InitializeAsync(db);
        await HomePartnersSectionInitializer.InitializeAsync(db); await HomeTestimonialsSectionInitializer.InitializeAsync(db);
        await PartnerInitializer.InitializeAsync(db); await TestimonialInitializer.InitializeAsync(db);
        await ServiceInitializer.InitializeAsync(db);
        await ProjectInitializer.InitializeAsync(db, projectDefaults);
        (await db.Projects.FirstAsync()).Name = "Existing edited Project";
        (await db.Services.FirstAsync()).Name = "Existing shared Service edit";
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
            Hero = await db.HomeHeroSettings.AsNoTracking().ToArrayAsync(), Welcome = await db.HomeWelcomeSettings.AsNoTracking().ToArrayAsync(),
            Services = await db.HomeServicesSectionSettings.AsNoTracking().ToArrayAsync(), Projects = await db.HomeProjectsSectionSettings.AsNoTracking().ToArrayAsync(),
            Team = await db.HomeTeamSectionSettings.AsNoTracking().ToArrayAsync(), Stats = await db.HomeStatistics.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            PartnersIntro = await db.HomePartnersSectionSettings.AsNoTracking().ToArrayAsync(), TestimonialsIntro = await db.HomeTestimonialsSectionSettings.AsNoTracking().ToArrayAsync(),
            Partners = await db.Partners.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Testimonials = await db.Testimonials.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            SharedServices = await db.Services.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), FeaturedServices = await db.HomeFeaturedServices.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => new { x.ServiceId, x.DisplayOrder }).ToArrayAsync(),
            SharedProjects = await db.Projects.AsNoTracking().Include(x => x.Gallery).Include(x => x.Features).OrderBy(x => x.Id).Select(x => x.ToDetail()).ToArrayAsync(),
            FeaturedProjects = await db.HomeFeaturedProjects.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => new { x.ProjectId, x.DisplayOrder }).ToArrayAsync(),
            Users = await db.Users.AsNoTracking().ToArrayAsync(), Roles = await db.Roles.AsNoTracking().ToArrayAsync(), Membership = await db.UserRoles.AsNoTracking().ToArrayAsync()
        });
        var before = await Previous();
        await migrator.MigrateAsync(); await MemberInitializer.InitializeAsync(db, defaults);
        Check(await Previous() == before, "Additive migration leaves Identity/hash/roles, all eight Home CMS slices, Testimonials, Partners, Services and Projects untouched.");
        Check(await db.TestimonialInitializationStates.CountAsync() == 1 && await db.PartnerInitializationStates.CountAsync() == 1, "Previous initialization markers intact.");
        Check(await db.Members.CountAsync() == 6 && await db.HomeFeaturedMembers.CountAsync() == 4 && !db.Database.HasPendingModelChanges(), "New Member tables initialized without model drift.");
        db.HomeFeaturedMembers.Add(new HomeFeaturedMember { MemberId = int.MaxValue, DisplayOrder = 1 });
        var rejected = false;
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { rejected = true; }
        Check(rejected, "Foreign key rejects nonexistent Member selection.");
        db.ChangeTracker.Clear();
        var duplicate = new MemberEntity { DisplayOrder = 8 };
        duplicate.SetContent(MemberEditModel.FromContent((await defaults.GetDetailsAsync())[0]));
        duplicate.Slug = duplicate.Slug.ToUpperInvariant();
        db.Members.Add(duplicate); rejected = false;
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { rejected = true; }
        Check(rejected, "Database unique index rejects duplicate slugs independently of service validation.");
    }

    private static async Task CheckAtomicInitializationAsync(MemberCatalog defaults)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailMemberMarker BEFORE INSERT ON MemberInitializationState BEGIN SELECT RAISE(ABORT, 'isolated test failure'); END;");
        var failed = false;
        try { await MemberInitializer.InitializeAsync(db, defaults); }
        catch (DbUpdateException) { failed = true; }
        db.ChangeTracker.Clear();
        Check(failed && await db.Members.CountAsync() == 0 && await db.HomeFeaturedMembers.CountAsync() == 0 && !await db.MemberInitializationStates.AnyAsync(),
            "Failed initialization rolls back both default rows and marker together.");
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailMemberMarker;");
        await MemberInitializer.InitializeAsync(db, defaults);
        Check(await db.Members.CountAsync() == 6 && await db.HomeFeaturedMembers.CountAsync() == 4 && await db.MemberInitializationStates.CountAsync() == 1,
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
