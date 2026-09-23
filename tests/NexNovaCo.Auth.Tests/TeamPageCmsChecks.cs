using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Components.Pages.Dashboard;
using NexNovaCo.Web.Components.Shared;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class TeamPageCmsChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        using var adminClient = app.NewClient();
        var loginProbe = await adminClient.GetAsync("/admin/login");
        if (!loginProbe.IsSuccessStatusCode) throw new InvalidOperationException(await loginProbe.Content.ReadAsStringAsync());
        await Login(adminClient, AuthFactory.Email, app.Password);
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
        var principal = await signIn.CreateUserPrincipalAsync(admin);
        var auth = new TestAuth(principal);
        var media = new LocalMediaStorageService(app.Services.GetRequiredService<MediaFilePaths>(), factory, auth, options);
        var logger = new TestLogger();
        var service = new TeamPageCmsService(factory, auth, options, media, logger);
        var sections = new[] { "Hero", "Section" };
        var types = new[] { typeof(TeamHeroEditor), typeof(TeamSectionEditor) };

        var baseline = await service.ReadPublicAsync();
        Check(JsonSerializer.Serialize(baseline) == JsonSerializer.Serialize(TeamPageDefaults.Content), "Fresh Team content exactly matches approved typed defaults.");
        await RestartAsync(app);
        Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(baseline), "Restart does not duplicate/default-overwrite Team content.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(!db.Database.HasPendingModelChanges(), "Team migration matches model.");
        }
        await MigrationAsync(factory);
        Check((await anonymous.GetStringAsync("/team")).Contains("http://localhost/image/team/ourteam-header.jpg") ||
              (await anonymous.GetStringAsync("/team")).Contains("https://localhost/image/team/ourteam-header.jpg"), "CSS media uses base-aware absolute URLs, not stylesheet-relative paths.");

        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "team-viewer@example.invalid", UserName = "team-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated non-Admin viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);
        var routes = sections.Select(x => "/dashboard/content/team/" + (x == "Section" ? "overview" : "hero"))
            .Concat(new[] { "/dashboard/content/team", "/dashboard/content/team/overview",
                "/dashboard/content/shared-team", "/dashboard/content/shared-team/new", "/dashboard/content/shared-team/1", "/dashboard/content/shared-team/home-featured" });
        foreach (var route in routes)
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous Team route challenges: " + route);
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin Team route denied: " + route);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var response = await adminClient.GetAsync(route);
                Check(response.IsSuccessStatusCode || route == "/dashboard/content/team" && response.StatusCode == HttpStatusCode.Redirect, "Admin direct/refresh route: " + route);
            }
        }
        Check((await adminClient.GetAsync("/dashboard/content/shared-team")).IsSuccessStatusCode, "Shared Team Admin smoke.");
        Check((await anonymous.GetAsync("/")).IsSuccessStatusCode, "Home anonymous shared smoke.");

        string prior;
        await using (var db = await factory.CreateDbContextAsync()) prior = await SnapshotPriorTablesAsync(db);
        for (var i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            var model = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            var editor = Activator.CreateInstance(types[i])!;
            WireEditor(editor, model, service);
            Check(!Dirty(editor), section + " loads clean.");
            var title = model.GetType().GetProperty("Title")!;
            title.SetValue(model, section + " isolated edit");
            model.GetType().GetProperty("Description")!.SetValue(model, section + " isolated description");
            if (model is TeamHeroEditModel hero)
            {
                hero.CtaLabel = "Isolated CTA";
                hero.CtaHref = "/team#Team";
            }
            if (model is TeamSectionEditModel intro)
            {
                intro.Eyebrow = "Isolated eyebrow";
                intro.Introduction = "Isolated introduction";
                intro.Highlight = "Isolated highlight";
                intro.CtaLabel = "Isolated section CTA";
                intro.CtaHref = "/contact";
            }
            Check(Dirty(editor), section + " becomes dirty.");
            // Actual handler failure must preserve the local model and dirty state.
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockTeamSave BEFORE UPDATE ON Team" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, section + " failed save stays dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockTeamSave;");
            title.SetValue(model, "");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, section + " server validation failure stays dirty.");
            title.SetValue(model, section + " isolated edit");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, section + " successful save clean.");
            var html = await anonymous.GetStringAsync("/team");
            Check(html.Contains(section + " isolated edit") && html.Contains(section + " isolated description"), section + " public SQLite fields visible.");
            if (model is TeamHeroEditModel)
                Check(html.Contains("Isolated CTA") && html.Contains("href=\"/team#Team\""), "Hero CTA renders from SQLite.");
            if (model is TeamSectionEditModel)
                Check(new[] { "Isolated eyebrow", "Isolated introduction", "Isolated highlight", "Isolated section CTA", "href=\"/contact\"" }.All(html.Contains), "Every intro text/CTA field renders from SQLite.");
            await RestartAsync(app);
            var persisted = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            Check(JsonSerializer.Serialize(persisted) == JsonSerializer.Serialize(model), section + " survives host restart.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + section + "Async", approved, CancellationToken.None);
        }
        await ImagesAsync(service, media, factory, anonymous);
        await FallbackAsync(service, factory, anonymous);
        Check(logger.Errors >= 2, "Each unavailable settings slice logs an error.");

        await using (var db = await factory.CreateDbContextAsync())
            Check(await SnapshotPriorTablesAsync(db) == prior, "Page edits, restores, media, fallback and restarts leave every prior CMS/Identity/shared row unchanged.");
        await SharedAndEmptyAsync(app, service, factory, anonymous, adminClient);
        var operations = new Func<Task>[]
        {
            () => service.GetHeroForEditAsync(),
            () => service.SaveHeroAsync(TeamHeroEditModel.Approved()),
            () => service.GetSectionForEditAsync(),
            () => service.SaveSectionAsync(TeamSectionEditModel.Approved())
        };
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), await signIn.CreateUserPrincipalAsync(viewerUser) })
        {
            auth.User = identity;
            foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Every Admin read/write denies anonymous/non-Admin service invocation.");
            Check((await service.ReadPublicAsync()).Hero.Title == TeamPageDefaults.Content.Hero.Title, "Public Team read remains anonymous.");
        }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin");
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Live role revocation enforced.");
        await users.AddToRoleAsync(admin, "Admin");
        await users.UpdateSecurityStampAsync(admin);
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Security-stamp revocation enforced.");
        Validation();
    }

    private static async Task SharedAndEmptyAsync(AuthFactory app, TeamPageCmsService page, IDbContextFactory<ApplicationDbContext> factory, HttpClient client, HttpClient admin)
    {
        var originalDetail = await client.GetStringAsync("/team/emilyjohnson");
        Check(originalDetail.Contains("Emily Johnson") && originalDetail.Contains("Skills"), "Shared Member detail smoke.");
        Check((await admin.GetStringAsync("/dashboard/content/team/overview")).Contains("/dashboard/content/shared-team"), "Team Section links canonical member catalog.");
        foreach (var suffix in new[] { "new", "1", "home-featured" })
        {
            var response = await admin.GetAsync("/dashboard/content/team/" + suffix);
            Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString().EndsWith("/dashboard/content/shared-team/" + suffix), "Legacy shared route redirects: " + suffix);
        }
        var edited = TeamSectionEditModel.Approved(); edited.Title = "Team only intro";
        await page.SaveSectionAsync(edited);
        Check((await client.GetStringAsync("/team")).Contains(edited.Title) && !(await client.GetStringAsync("/")).Contains(edited.Title), "Team intro does not change Home intro.");
        Check(!(await client.GetStringAsync("/team/emilyjohnson")).Contains(edited.Title), "Team intro does not leak into Member Detail.");
        await page.SaveSectionAsync(TeamSectionEditModel.Approved());
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Members.ExecuteDeleteAsync();
        }
        await RestartAsync(app);
        foreach (var route in new[] { "/team", "/" })
        {
            var html = await client.GetStringAsync(route);
            Check(!html.Contains("team_member_box"), "Intentionally empty member catalog produces no fake cards on " + route);
        }
        Check((await client.GetAsync("/team/emilyjohnson")).StatusCode == HttpStatusCode.NotFound, "Deleted shared member detail returns not found.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(!await db.Members.AnyAsync() && !await db.HomeFeaturedMembers.AnyAsync(), "Restart keeps catalog/featured relations intentionally empty.");
    }

    private static async Task ImagesAsync(TeamPageCmsService service, IMediaStorageService media, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.TeamHeroDefault));
        foreach (var kind in new[] { MediaKind.TeamHero, MediaKind.TeamSection })
        {
            var section = kind == MediaKind.TeamHero ? "Hero" : "Section";
            var model = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            object editor = kind == MediaKind.TeamHero ? new TeamHeroEditor() : new TeamSectionEditor();
            WireEditor(editor, model, service);
            var field = new CmsImageField();
            SetProperty(field, "Media", media); SetProperty(field, "Value", model.GetType().GetProperty("ImagePath")!.GetValue(model));
            SetProperty(field, "Kind", kind);
            var selected = await media.ReadAsync(new TestFile(bytes), kind);
            SetField(field, "_pending", selected); SetField(field, "_selectedName", selected.FileName);
            SetField(editor, "_imageField", field);
            Check(Dirty(editor), section + " image-only selection dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockTeamImage BEFORE UPDATE ON Team" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && field.HasPendingSelection, section + " failed DB save keeps selected image/dirty state.");
            var path = (string)GetField(field, "_storedPath")!;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockTeamImage;");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && !field.HasPendingSelection && MediaPolicy.IsGenerated(path, kind), section + " retry reuses validated upload and marks clean.");
            var image = await client.GetAsync("/" + path);
            Check(image.IsSuccessStatusCode && image.Content.Headers.ContentType!.MediaType == "image/jpeg" && image.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "Team media served as safe raster.");
            Check((await client.GetStringAsync("/team")).Contains(path), section + " public image path from database.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + section + "Async", approved, CancellationToken.None);
            field.Dispose();
        }
    }

    private static async Task FallbackAsync(TeamPageCmsService service, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var hero = TeamHeroEditModel.Approved(); hero.Title = "Stored Hero";
        var intro = TeamSectionEditModel.Approved(); intro.Title = "Stored Intro";
        await service.SaveHeroAsync(hero); await service.SaveSectionAsync(intro);
        var stored = await service.ReadPublicAsync();
        foreach (var table in new[] { "TeamHeroSettings", "TeamSectionSettings" })
        {
            await using var db = await factory.CreateDbContextAsync();
            var before = JsonSerializer.Serialize(new { Hero = await db.TeamHeroSettings.AsNoTracking().SingleAsync(), Intro = await db.TeamSectionSettings.AsNoTracking().SingleAsync() });
            await ExecuteFixtureDdlAsync(db, "ALTER TABLE " + table + " RENAME TO IsolatedUnavailable;");
            try
            {
                var expected = table == "TeamHeroSettings" ? stored with { Hero = TeamPageDefaults.Content.Hero }
                    : stored with { Eyebrow = TeamPageDefaults.Content.Eyebrow, Introduction = TeamPageDefaults.Content.Introduction };
                Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(expected), table + " failure falls back only its slice.");
                Check((await client.GetAsync("/team")).IsSuccessStatusCode, table + " missing slice does not crash public.");
            }
            finally { await ExecuteFixtureDdlAsync(db, "ALTER TABLE IsolatedUnavailable RENAME TO " + table + ";"); }
            var after = JsonSerializer.Serialize(new { Hero = await db.TeamHeroSettings.AsNoTracking().SingleAsync(), Intro = await db.TeamSectionSettings.AsNoTracking().SingleAsync() });
            Check(after == before, "Fallback did not write settings or timestamps.");
        }
        await service.SaveHeroAsync(TeamHeroEditModel.Approved()); await service.SaveSectionAsync(TeamSectionEditModel.Approved());
    }

    // Only compile-time fixture table names enter this helper; DDL identifiers cannot be SQL parameters.
    private static Task ExecuteFixtureDdlAsync(ApplicationDbContext db, string fixtureSql) => db.Database.ExecuteSqlRawAsync(fixtureSql);
    private static async Task MigrationAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var teamIndex = Array.FindIndex(migrations, x => x.EndsWith("_AddTeamPageCms", StringComparison.Ordinal));
        Check(teamIndex >= 0, "Additive Team migration exists.");
        // This is exclusively AuthFactory's disposable database, not the normal developer database.
        await db.GetService<IMigrator>().MigrateAsync(migrations[teamIndex - 1]);
        var before = await SnapshotPriorTablesAsync(db);
        await db.GetService<IMigrator>().MigrateAsync(migrations[teamIndex]);
        await TeamPageInitializer.InitializeAsync(db);
        Check(await SnapshotPriorTablesAsync(db) == before, "Upgrade preserves every prior Identity/Home/shared table, row, timestamp and account.");
        await db.Database.MigrateAsync();
        await ContactPageInitializer.InitializeAsync(db);
        await GlobalSiteInitializer.InitializeAsync(db);
        Check(!db.Database.HasPendingModelChanges(), "Upgrade leaves no pending model changes.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[teamIndex]], db.Database.ProviderName!);
        Check(migration.UpOperations.Count == 2 && migration.UpOperations.All(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation), "Team migration only creates its tables/indexes; no destructive operations.");
    }
    private static async Task<string> SnapshotPriorTablesAsync(ApplicationDbContext db)
    {
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'Team%Settings' AND name NOT LIKE '__EF%' AND name <> 'sqlite_sequence' ORDER BY name").ToArrayAsync();
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();
        var snapshot = new List<string>();
        foreach (var name in names)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + name.Replace("\"", "\"\"") + "\" ORDER BY rowid";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync()) { var values = new object[reader.FieldCount]; reader.GetValues(values); rows.Add(JsonSerializer.Serialize(values)); }
            snapshot.Add(name + ":" + string.Join("|", rows));
        }
        return string.Join("\n", snapshot);
    }
    private static async Task RestartAsync(AuthFactory app)
    {
        await using var restart = new AuthFactory(app.Password, databasePath: app.DatabasePath);
        using var client = restart.NewClient();
        Check((await client.GetAsync("/team")).IsSuccessStatusCode, "Separate host restart on isolated existing database.");
    }
    private static void Validation()
    {
        foreach (var route in new[] { "/", "team", "/projects/nexconnect", "/team/alex", "team#Team", "/team#Team" })
            Check(new TeamPageCtaRouteAttribute().IsValid(route), "Approved internal CTA accepted.");
        foreach (var route in new[] { "//evil.test", "https://evil.test", "javascript:alert(1)", "/dashboard", "/team?x=1", "/team#fake", "/team\n", "/../admin", "/%2fadmin" })
            Check(!new TeamPageCtaRouteAttribute().IsValid(route), "Unsafe CTA rejected.");
        var longHero = TeamHeroEditModel.Approved(); longHero.Title = new string('x', 81);
        var emptyBrand = TeamSectionEditModel.Approved(); emptyBrand.Description = "";
        foreach (var model in new object[] { longHero, emptyBrand })
            Check(!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true), "Required fields and length limits enforced.");
    }
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static void WireEditor(object editor, object model, ITeamPageCmsService service)
    {
        SetField(editor, "_model", model); SetProperty(editor, "TeamPageService", service);
        var logger = Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(editor.GetType()));
        SetProperty(editor, "Logger", logger);
        ((EditorSnapshot)GetField(editor, "_snapshot")!).Capture((string[])GetProperty(editor, "CurrentValues")!);
    }
    private static void SetField(object target, string name, object? value) => target.GetType().GetField(name, Flags)!.SetValue(target, value);
    private static object? GetField(object target, string name) => target.GetType().GetField(name, Flags)!.GetValue(target);
    private static void SetProperty(object target, string name, object? value) => target.GetType().GetProperty(name, Flags)!.SetValue(target, value);
    private static object? GetProperty(object target, string name) => target.GetType().GetProperty(name, Flags)!.GetValue(target);
    private static bool Dirty(object editor) => (bool)GetProperty(editor, "IsDirty")!;
    private static async Task<object?> CallAsync(object target, string method, params object[] args)
    {
        var task = (Task)target.GetType().GetMethod(method, Flags)!.Invoke(target, args)!;
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }
    private static async Task RejectAsync<T>(Func<Task> action, string message) where T : Exception
    {
        try { await action(); } catch (T) { Check(true, message); return; }
        Check(false, message);
    }
    private sealed class TestLogger : ILogger<TeamPageCmsService>
    {
        public int Errors { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level == LogLevel.Error) Errors++;
        }
    }
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public ClaimsPrincipal User { get; set; } = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
    }
    private sealed class TestFile(byte[] bytes) : IBrowserFile
    {
        public string Name => "team-test.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes, false);
    }
}
