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

internal static class ProjectsPageCmsChecks
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
        var service = new ProjectsPageCmsService(factory, auth, options, media, logger);
        var sections = new[] { "Hero", "Testimonials" };
        var types = new[] { typeof(ProjectsHeroEditor), typeof(ProjectsTestimonialsEditor) };

        var baseline = await service.ReadPublicAsync();
        Check(JsonSerializer.Serialize(baseline) == JsonSerializer.Serialize(ProjectsPageDefaults.Content), "Fresh Projects content exactly matches approved typed defaults.");
        await RestartAsync(app);
        Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(baseline), "Restart does not duplicate/default-overwrite Projects content.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(!db.Database.HasPendingModelChanges(), "Projects migration matches model.");
        }
        await MigrationAsync(factory);
        Check((await anonymous.GetStringAsync("/projects")).Contains("http://localhost/image/home/inner-banner.jpg") ||
              (await anonymous.GetStringAsync("/projects")).Contains("https://localhost/image/home/inner-banner.jpg"), "CSS media uses base-aware absolute URLs, not stylesheet-relative paths.");

        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "projects-viewer@example.invalid", UserName = "projects-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated non-Admin viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);
        var routes = sections.Select(x => "/dashboard/content/projects/" + x.ToLowerInvariant())
            .Concat(new[] { "/dashboard/content/projects", "/dashboard/content/projects/overview",
                "/dashboard/content/shared-projects", "/dashboard/content/shared-projects/new", "/dashboard/content/shared-projects/1", "/dashboard/content/shared-projects/home-featured", "/dashboard/content/testimonials" });
        foreach (var route in routes)
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous Projects route challenges: " + route);
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin Projects route denied: " + route);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var response = await adminClient.GetAsync(route);
                Check(response.IsSuccessStatusCode || route == "/dashboard/content/projects" && response.StatusCode == HttpStatusCode.Redirect, "Admin direct/refresh route: " + route);
            }
        }
        Check((await adminClient.GetAsync("/dashboard/content/shared-projects")).IsSuccessStatusCode, "Shared Projects Admin smoke.");
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
            if (model is ProjectsHeroEditModel hero)
            {
                hero.MobileTitle = "Isolated mobile title";
                hero.CtaLabel = "Isolated CTA";
                hero.CtaHref = "/projects#Project";
            }
            Check(Dirty(editor), section + " becomes dirty.");
            // Actual handler failure must preserve the local model and dirty state.
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockProjectsSave BEFORE UPDATE ON Projects" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, section + " failed save stays dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockProjectsSave;");
            title.SetValue(model, "");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, section + " server validation failure stays dirty.");
            title.SetValue(model, section + " isolated edit");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, section + " successful save clean.");
            var html = await anonymous.GetStringAsync("/projects");
            Check(html.Contains(section + " isolated edit") && html.Contains(section + " isolated description"), section + " public SQLite fields visible.");
            if (model is ProjectsHeroEditModel)
                Check(html.Contains("Isolated mobile title") && html.Contains("Isolated CTA") && html.Contains("href=\"/projects#Project\""), "Hero mobile title and CTA render from SQLite.");
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
            () => service.SaveHeroAsync(ProjectsHeroEditModel.Approved()),
            () => service.GetTestimonialsForEditAsync(),
            () => service.SaveTestimonialsAsync(ProjectsTestimonialsEditModel.Approved())
        };
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), await signIn.CreateUserPrincipalAsync(viewerUser) })
        {
            auth.User = identity;
            foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Every Admin read/write denies anonymous/non-Admin service invocation.");
            Check((await service.ReadPublicAsync()).Hero.Title == ProjectsPageDefaults.Content.Hero.Title, "Public Projects read remains anonymous.");
        }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin");
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Live role revocation enforced.");
        await users.AddToRoleAsync(admin, "Admin");
        await users.UpdateSecurityStampAsync(admin);
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Security-stamp revocation enforced.");
        Validation();
    }

    private static async Task SharedAndEmptyAsync(AuthFactory app, ProjectsPageCmsService page, IDbContextFactory<ApplicationDbContext> factory, HttpClient client, HttpClient admin)
    {
        Check((await client.GetAsync("/projects/nexconnect")).IsSuccessStatusCode, "Shared Project detail smoke.");
        Check((await admin.GetStringAsync("/dashboard/content/projects/overview")).Contains("/dashboard/content/shared-projects"), "Projects Section links canonical catalog.");
        Check((await admin.GetStringAsync("/dashboard/content/projects/testimonials")).Contains("/dashboard/content/testimonials"), "Testimonials editor links canonical catalog.");
        foreach (var suffix in new[] { "new", "1", "home-featured" })
        {
            var response = await admin.GetAsync("/dashboard/content/projects/" + suffix);
            Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.ToString().EndsWith("/dashboard/content/shared-projects/" + suffix), "Legacy shared route redirects: " + suffix);
        }
        var edited = ProjectsTestimonialsEditModel.Approved(); edited.Title = "Projects only brand";
        await page.SaveTestimonialsAsync(edited);
        Check((await client.GetStringAsync("/projects")).Contains(edited.Title) && !(await client.GetStringAsync("/")).Contains(edited.Title), "Projects brand does not change Home brand.");
        await page.SaveTestimonialsAsync(ProjectsTestimonialsEditModel.Approved());
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Projects.ExecuteDeleteAsync();
            await db.Testimonials.ExecuteDeleteAsync();
        }
        await RestartAsync(app);
        foreach (var route in new[] { "/projects", "/" })
        {
            var html = await client.GetStringAsync(route);
            Check(!html.Contains("Client testimonials"), "Intentionally empty testimonials omit carousel on " + route);
            if (route == "/projects")
                Check(!html.Contains("project_item") && html.Contains("class=\"pagination\""), "Empty Projects has no fake cards and retains decorative pagination.");
        }
        await using (var db = await factory.CreateDbContextAsync())
            Check(!await db.Projects.AnyAsync() && !await db.Testimonials.AnyAsync(), "Restart keeps both shared collections intentionally empty.");
    }

    private static async Task ImagesAsync(ProjectsPageCmsService service, IMediaStorageService media, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.ProjectsHeroDefault));
        foreach (var kind in new[] { MediaKind.ProjectsHero })
        {
            var section = "Hero";
            var model = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            object editor = new ProjectsHeroEditor();
            WireEditor(editor, model, service);
            var field = new CmsImageField();
            SetProperty(field, "Media", media); SetProperty(field, "Value", model.GetType().GetProperty("ImagePath")!.GetValue(model));
            SetProperty(field, "Kind", kind);
            var selected = await media.ReadAsync(new TestFile(bytes), kind);
            SetField(field, "_pending", selected); SetField(field, "_selectedName", selected.FileName);
            SetField(editor, "_imageField", field);
            Check(Dirty(editor), section + " image-only selection dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockProjectsImage BEFORE UPDATE ON Projects" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && field.HasPendingSelection, section + " failed DB save keeps selected image/dirty state.");
            var path = (string)GetField(field, "_storedPath")!;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockProjectsImage;");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && !field.HasPendingSelection && MediaPolicy.IsGenerated(path, kind), section + " retry reuses validated upload and marks clean.");
            var image = await client.GetAsync("/" + path);
            Check(image.IsSuccessStatusCode && image.Content.Headers.ContentType!.MediaType == "image/jpeg" && image.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "Projects media served as safe raster.");
            Check((await client.GetStringAsync("/projects")).Contains(path), section + " public image path from database.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + section + "Async", approved, CancellationToken.None);
            field.Dispose();
        }
    }

    private static async Task FallbackAsync(ProjectsPageCmsService service, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var before = JsonSerializer.Serialize(await service.ReadPublicAsync());
        foreach (var table in new[] { "ProjectsHeroSettings", "ProjectsTestimonialsSettings" })
        {
            await using var db = await factory.CreateDbContextAsync();
            await ExecuteFixtureDdlAsync(db, "ALTER TABLE " + table + " RENAME TO IsolatedUnavailable;");
            try
            {
                Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == before, table + " read failure uses approved slice defaults.");
                Check((await client.GetAsync("/projects")).IsSuccessStatusCode, table + " missing slice does not crash public.");
            }
            finally { await ExecuteFixtureDdlAsync(db, "ALTER TABLE IsolatedUnavailable RENAME TO " + table + ";"); }
            Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == before, "Fallback did not persist or overwrite content.");
        }
    }

    // Only compile-time fixture table names enter this helper; DDL identifiers cannot be SQL parameters.
    private static Task ExecuteFixtureDdlAsync(ApplicationDbContext db, string fixtureSql) => db.Database.ExecuteSqlRawAsync(fixtureSql);
    private static async Task MigrationAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var projectsIndex = Array.FindIndex(migrations, x => x.EndsWith("_AddProjectsPageCms", StringComparison.Ordinal));
        Check(projectsIndex == 18, "One additive Projects migration.");
        // This is exclusively AuthFactory's disposable database, not the normal developer database.
        await db.GetService<IMigrator>().MigrateAsync(migrations[projectsIndex - 1]);
        var before = await SnapshotPriorTablesAsync(db);
        await db.GetService<IMigrator>().MigrateAsync(migrations[projectsIndex]);
        await ProjectsPageInitializer.InitializeAsync(db);
        Check(await SnapshotPriorTablesAsync(db) == before, "Upgrade preserves every prior Identity/Home/shared table, row, timestamp and account.");
        await db.Database.MigrateAsync();
        await TeamPageInitializer.InitializeAsync(db);
        await ContactPageInitializer.InitializeAsync(db);
        Check(!db.Database.HasPendingModelChanges(), "Upgrade leaves no pending model changes.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[projectsIndex]], db.Database.ProviderName!);
        Check(migration.UpOperations.Count == 2 && migration.UpOperations.All(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation), "Projects migration only creates its tables/indexes; no destructive operations.");
    }
    private static async Task<string> SnapshotPriorTablesAsync(ApplicationDbContext db)
    {
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'Projects%Settings' AND name NOT LIKE 'Team%Settings' AND name NOT LIKE '__EF%' AND name <> 'sqlite_sequence' ORDER BY name").ToArrayAsync();
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
        Check((await client.GetAsync("/projects")).IsSuccessStatusCode, "Separate host restart on isolated existing database.");
    }
    private static void Validation()
    {
        foreach (var route in new[] { "/", "projects", "/projects/nexconnect", "/team/alex", "projects#Project", "/projects#Project" })
            Check(new ProjectsPageCtaRouteAttribute().IsValid(route), "Approved internal CTA accepted.");
        foreach (var route in new[] { "//evil.test", "https://evil.test", "javascript:alert(1)", "/dashboard", "/projects?x=1", "/projects#fake", "/projects\n", "/../admin", "/%2fadmin" })
            Check(!new ProjectsPageCtaRouteAttribute().IsValid(route), "Unsafe CTA rejected.");
        var longHero = ProjectsHeroEditModel.Approved(); longHero.Title = new string('x', 81);
        var emptyBrand = ProjectsTestimonialsEditModel.Approved(); emptyBrand.Description = "";
        foreach (var model in new object[] { longHero, emptyBrand })
            Check(!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true), "Required fields and length limits enforced.");
    }
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static void WireEditor(object editor, object model, IProjectsPageCmsService service)
    {
        SetField(editor, "_model", model); SetProperty(editor, "ProjectsPageService", service);
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
    private sealed class TestLogger : ILogger<ProjectsPageCmsService>
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
        public string Name => "projects-test.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes, false);
    }
}
