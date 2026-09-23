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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Components.Pages.Dashboard;
using NexNovaCo.Web.Components.Shared;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class AboutCmsChecks
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
        var service = new AboutCmsService(factory, auth, options, media, NullLogger<AboutCmsService>.Instance);
        var partnerService = new PartnerContentService(factory, auth, options, NullLogger<PartnerContentService>.Instance, media);
        var about = new AboutContentService(service, partnerService);
        var sections = new[] { "Hero", "Story", "Vision", "Timeline", "Mission", "Partners" };
        var types = new[] { typeof(AboutHeroEditor), typeof(AboutStoryEditor), typeof(AboutVisionEditor), typeof(AboutTimelineEditor), typeof(AboutMissionEditor), typeof(AboutPartnersEditor) };

        var baseline = await service.ReadPublicAsync();
        Check(JsonSerializer.Serialize(baseline) == JsonSerializer.Serialize(AboutDefaults.Content with { Partners = [] }), "Fresh About content exactly matches approved typed defaults.");
        await RestartAsync(app);
        Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(baseline), "Restart does not duplicate/default-overwrite About content.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(!db.Database.HasPendingModelChanges(), "About migration matches model.");
            Check(await db.AboutTimelineInitializationStates.CountAsync() == 1 && await db.AboutMissionPointInitializationStates.CountAsync() == 1, "Both collections have persistent seed markers.");
        }
        await MigrationAsync(factory);

        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "about-viewer@example.invalid", UserName = "about-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated non-Admin viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);
        var routes = sections.Select(x => "/dashboard/content/about/" + x.ToLowerInvariant())
            .Concat(new[] { "/dashboard/content/about", "/dashboard/content/about/timeline/new", "/dashboard/content/about/timeline/1",
                "/dashboard/content/about/mission/points/new", "/dashboard/content/about/mission/points/1" });
        foreach (var route in routes)
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous About route challenges: " + route);
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin About route denied: " + route);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var response = await adminClient.GetAsync(route);
                Check(response.IsSuccessStatusCode || route == "/dashboard/content/about" && response.StatusCode == HttpStatusCode.Redirect, "Admin direct/refresh route: " + route);
            }
        }
        Check((await adminClient.GetStringAsync("/dashboard/content/about/timeline/2147483647")).Contains("no longer exists"), "Missing timeline item safe.");
        Check((await adminClient.GetStringAsync("/dashboard/content/about/mission/points/2147483647")).Contains("no longer exists"), "Missing mission point safe.");
        Check((await adminClient.GetAsync("/dashboard/content/partners")).IsSuccessStatusCode, "Shared Partners Admin smoke.");
        Check((await anonymous.GetAsync("/")).IsSuccessStatusCode, "Home anonymous shared smoke.");

        for (var i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            var model = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            var editor = Activator.CreateInstance(types[i])!;
            WireEditor(editor, model, service);
            Check(!Dirty(editor), section + " loads clean.");
            var title = model.GetType().GetProperty("Title")!;
            title.SetValue(model, section + " isolated edit");
            Check(Dirty(editor), section + " becomes dirty.");
            // Actual handler failure must preserve the local model and dirty state.
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockAboutSave BEFORE UPDATE ON About" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, section + " failed save stays dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockAboutSave;");
            title.SetValue(model, "");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, section + " server validation failure stays dirty.");
            title.SetValue(model, section + " isolated edit");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, section + " successful save clean.");
            Check((await anonymous.GetStringAsync("/about")).Contains(section + " isolated edit"), section + " public SQLite edit visible.");
            await RestartAsync(app);
            var persisted = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            Check(JsonSerializer.Serialize(persisted) == JsonSerializer.Serialize(model), section + " survives host restart.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + section + "Async", approved, CancellationToken.None);
        }
        await CollectionsAsync(app, service, anonymous);
        await ImagesAsync(service, media, factory, anonymous);
        await CollectionEditorAsync(service);
        await FallbackAsync(service, factory, anonymous);

        var firstPartner = (await partnerService.ListAsync())[0];
        var partnerOriginal = await partnerService.GetForEditAsync(firstPartner.Id);
        var editedPartner = await partnerService.GetForEditAsync(firstPartner.Id);
        editedPartner.Name = "Shared About QA Partner";
        await partnerService.UpdateAsync(firstPartner.Id, editedPartner);
        Check((await about.GetAsync()).Partners.Any(x => x.Name == editedPartner.Name), "About uses canonical shared Partner service.");
        Check((await anonymous.GetStringAsync("/")).Contains(editedPartner.Name) && (await anonymous.GetStringAsync("/about")).Contains(editedPartner.Name), "Shared Partner edit visible on Home and About.");
        await partnerService.UpdateAsync(firstPartner.Id, partnerOriginal);

        var operations = new Func<Task>[]
        {
            () => service.GetHeroForEditAsync(), () => service.GetStoryForEditAsync(), () => service.GetVisionForEditAsync(),
            () => service.GetTimelineForEditAsync(), () => service.GetMissionForEditAsync(), () => service.GetPartnersForEditAsync(),
            () => service.SaveHeroAsync(AboutHeroEditModel.Approved()), () => service.SaveStoryAsync(AboutStoryEditModel.Approved()),
            () => service.SaveVisionAsync(AboutVisionEditModel.Approved()), () => service.SaveTimelineAsync(AboutTimelineEditModel.Approved()),
            () => service.SaveMissionAsync(AboutMissionEditModel.Approved()), () => service.SavePartnersAsync(AboutPartnersEditModel.Approved()),
            () => service.ListTimelineAsync(), () => service.GetTimelineItemForEditAsync(1),
            () => service.CreateTimelineAsync(new() { Year = 2026, Description = "Denied" }), () => service.UpdateTimelineAsync(1, new() { Year = 2026, Description = "Denied" }),
            () => service.DeleteTimelineAsync(1), () => service.ReorderTimelineAsync([]),
            () => service.ListMissionPointAsync(), () => service.GetMissionPointItemForEditAsync(1),
            () => service.CreateMissionPointAsync(new() { Description = "Denied" }), () => service.UpdateMissionPointAsync(1, new() { Description = "Denied" }),
            () => service.DeleteMissionPointAsync(1), () => service.ReorderMissionPointAsync([])
        };
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), await signIn.CreateUserPrincipalAsync(viewerUser) })
        {
            auth.User = identity;
            foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Every Admin read/write denies anonymous/non-Admin service invocation.");
            Check((await service.ReadPublicAsync()).Hero.Title == AboutDefaults.Content.Hero.Title, "Public About read remains anonymous.");
        }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin");
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Live role revocation enforced.");
        await users.AddToRoleAsync(admin, "Admin");
        await users.UpdateSecurityStampAsync(admin);
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Security-stamp revocation enforced.");
        Validation();
    }

    private static async Task CollectionsAsync(AuthFactory app, AboutCmsService service, HttpClient client)
    {
        var timeline = await service.ListTimelineAsync();
        var points = await service.ListMissionPointAsync();
        Check(timeline.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1,3)) && points.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1,4)), "Approved collection order.");
        var tid = await service.CreateTimelineAsync(new() { Year = 2026, Description = "Added timeline" });
        await service.UpdateTimelineAsync(tid, new() { Year = 2030, Description = "Updated timeline" });
        var pid = await service.CreateMissionPointAsync(new() { Description = "Added commitment" });
        await service.UpdateMissionPointAsync(pid, new() { Description = "Updated commitment" });
        var timelineIds = (await service.ListTimelineAsync()).Select(x => x.Id).Reverse().ToArray();
        var pointIds = (await service.ListMissionPointAsync()).Select(x => x.Id).Reverse().ToArray();
        await service.ReorderTimelineAsync(timelineIds); await service.ReorderMissionPointAsync(pointIds);
        await RestartAsync(app);
        Check((await service.ListTimelineAsync()).Select(x => x.Id).SequenceEqual(timelineIds), "Timeline add/edit/reorder survive restart.");
        Check((await service.ListMissionPointAsync()).Select(x => x.Id).SequenceEqual(pointIds), "Mission add/edit/reorder survive restart.");
        var html = await client.GetStringAsync("/about");
        Check(html.Contains("Updated timeline") && html.Contains("Updated commitment") && html.Contains("timeline-partial"), "Fourth milestone and fifth point render without three-item assumption.");
        Check((await service.ReadPublicAsync()).Timeline.Milestones[0].Year == 2030 && (await service.ReadPublicAsync()).Mission.Commitments[0] == "Updated commitment", "Public collection order follows database.");
        await RejectAsync<ValidationException>(() => service.ReorderTimelineAsync([tid, tid]), "Duplicate/stale timeline order rejected.");
        await RejectAsync<ValidationException>(() => service.ReorderMissionPointAsync([pid, pid]), "Duplicate/stale point order rejected.");
        await service.DeleteTimelineAsync(tid); await service.DeleteMissionPointAsync(pid);
        await RestartAsync(app);
        Check((await service.ListTimelineAsync()).All(x => x.Id != tid) && (await service.ListMissionPointAsync()).All(x => x.Id != pid), "Delete one remains deleted after restart.");
        foreach (var item in await service.ListTimelineAsync()) await service.DeleteTimelineAsync(item.Id);
        foreach (var item in await service.ListMissionPointAsync()) await service.DeleteMissionPointAsync(item.Id);
        await RestartAsync(app);
        Check((await service.ListTimelineAsync()).Count == 0 && (await service.ListMissionPointAsync()).Count == 0, "Delete all remains empty after restart.");
        html = await client.GetStringAsync("/about");
        Check(!html.Contains("Company milestones") && !html.Contains("Our commitments") && !html.Contains("timeline-partial"), "Empty public lists omit connectors and placeholder points safely.");
        Check((await service.ReadPublicAsync()).Timeline.Milestones.Count == 0 && (await service.ReadPublicAsync()).Mission.Commitments.Count == 0, "Empty is not fallback.");
        tid = await service.CreateTimelineAsync(new() { Year = 2028, Description = "Only new timeline" });
        pid = await service.CreateMissionPointAsync(new() { Description = "Only new point" });
        await RestartAsync(app);
        Check((await service.ListTimelineAsync()).Single().Id == tid && (await service.ListMissionPointAsync()).Single().Id == pid, "Add after empty leaves only Admin-created items.");
        await service.DeleteTimelineAsync(tid); await service.DeleteMissionPointAsync(pid);
        // Restore approved content explicitly through CRUD, never by removing seed markers.
        foreach (var item in AboutDefaults.Content.Timeline.Milestones) await service.CreateTimelineAsync(new() { Year = item.Year, Description = item.Description });
        foreach (var item in AboutDefaults.Content.Mission.Commitments) await service.CreateMissionPointAsync(new() { Description = item });
    }

    private static async Task ImagesAsync(AboutCmsService service, IMediaStorageService media, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.AboutHeroDefault));
        foreach (var kind in new[] { MediaKind.AboutHero, MediaKind.AboutVision })
        {
            var section = kind == MediaKind.AboutHero ? "Hero" : "Vision";
            var model = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            object editor = kind == MediaKind.AboutHero ? new AboutHeroEditor() : new AboutVisionEditor();
            WireEditor(editor, model, service);
            var field = new CmsImageField();
            SetProperty(field, "Media", media); SetProperty(field, "Value", model.GetType().GetProperty("ImagePath")!.GetValue(model));
            SetProperty(field, "Kind", kind);
            var selected = await media.ReadAsync(new TestFile(bytes), kind);
            SetField(field, "_pending", selected); SetField(field, "_selectedName", selected.FileName);
            SetField(editor, "_imageField", field);
            Check(Dirty(editor), section + " image-only selection dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockAboutImage BEFORE UPDATE ON About" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && field.HasPendingSelection, section + " failed DB save keeps selected image/dirty state.");
            var path = (string)GetField(field, "_storedPath")!;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockAboutImage;");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && !field.HasPendingSelection && MediaPolicy.IsGenerated(path, kind), section + " retry reuses validated upload and marks clean.");
            var image = await client.GetAsync("/" + path);
            Check(image.IsSuccessStatusCode && image.Content.Headers.ContentType!.MediaType == "image/jpeg" && image.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "About media served as safe raster.");
            Check((await client.GetStringAsync("/about")).Contains(path), section + " public image path from database.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + section + "Async", approved, CancellationToken.None);
            field.Dispose();
        }
    }

    private static async Task CollectionEditorAsync(AboutCmsService service)
    {
        var t = (await service.ListTimelineAsync())[0];
        var p = (await service.ListMissionPointAsync())[0];
        foreach (var item in new (object Editor, object Model, int Id)[] {
            (new AboutTimelineItemEditor(), await service.GetTimelineItemForEditAsync(t.Id), t.Id),
            (new AboutMissionPointItemEditor(), await service.GetMissionPointItemForEditAsync(p.Id), p.Id) })
        {
            WireEditor(item.Editor, item.Model, service);
            SetField(item.Editor, "_editingId", (int?)item.Id); SetProperty(item.Editor, "Id", (int?)item.Id);
            Check(!Dirty(item.Editor), "Collection editor loads clean.");
            var prop = item.Model.GetType().GetProperty("Description")!;
            var original = prop.GetValue(item.Model);
            prop.SetValue(item.Model, "");
            Check(Dirty(item.Editor), "Collection edit dirty.");
            await CallAsync(item.Editor, "SaveAsync");
            Check(Dirty(item.Editor) && !(bool)GetField(item.Editor, "_saved")!, "Invalid collection save stays dirty.");
            prop.SetValue(item.Model, "Edited through actual handler");
            await CallAsync(item.Editor, "SaveAsync");
            Check(!Dirty(item.Editor) && (bool)GetField(item.Editor, "_saved")!, "Collection successful handler clean.");
            prop.SetValue(item.Model, original);
            await CallAsync(item.Editor, "SaveAsync");
        }
    }

    private static async Task FallbackAsync(AboutCmsService service, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var before = JsonSerializer.Serialize(await service.ReadPublicAsync());
        foreach (var table in new[] { "AboutHeroSettings", "AboutStorySettings", "AboutVisionSettings", "AboutTimelineSettings", "AboutMissionSettings", "AboutPartnersSettings", "AboutTimelineItems", "AboutMissionPointItems" })
        {
            await using var db = await factory.CreateDbContextAsync();
            await ExecuteFixtureDdlAsync(db, "ALTER TABLE " + table + " RENAME TO IsolatedUnavailable;");
            try
            {
                Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == before, table + " read failure uses approved slice defaults.");
                Check((await client.GetAsync("/about")).IsSuccessStatusCode, table + " missing slice does not crash public.");
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
        var aboutIndex = Array.FindIndex(migrations, x => x.EndsWith("_AddCompleteAboutCms", StringComparison.Ordinal));
        Check(aboutIndex == 16, "One additive About migration after the established foundation.");
        // This is exclusively AuthFactory's disposable database, not the normal developer database.
        await db.GetService<IMigrator>().MigrateAsync(migrations[aboutIndex - 1]);
        var before = await SnapshotPriorTablesAsync(db);
        await db.GetService<IMigrator>().MigrateAsync(migrations[aboutIndex]);
        await AboutInitializer.InitializeAsync(db);
        Check(await SnapshotPriorTablesAsync(db) == before, "Upgrade preserves every prior Identity/Home/shared table, row, timestamp and account.");
        await db.Database.MigrateAsync();
        await ServicesPageInitializer.InitializeAsync(db);
        Check(!db.Database.HasPendingModelChanges(), "Upgrade leaves no pending model changes.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[aboutIndex]], db.Database.ProviderName!);
        Check(migration.UpOperations.All(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation or Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation), "About migration only creates its tables/indexes; no destructive operations.");
    }
    private static async Task<string> SnapshotPriorTablesAsync(ApplicationDbContext db)
    {
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'About%' AND name NOT LIKE 'Services%Settings' AND name NOT LIKE 'ServiceBenefit%' AND name NOT LIKE 'ServiceProcess%' AND name NOT LIKE 'ServicePricing%' AND name NOT LIKE 'ServiceFaq%' AND name NOT LIKE '__EF%' AND name <> 'sqlite_sequence' ORDER BY name").ToArrayAsync();
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
        Check((await client.GetAsync("/about")).IsSuccessStatusCode, "Separate host restart on isolated existing database.");
    }
    private static void Validation()
    {
        foreach (var route in new[] { "/", "services", "/projects/nexconnect", "/team/alex", "about#About", "/about#About" })
            Check(new AboutCtaRouteAttribute().IsValid(route), "Approved internal CTA accepted.");
        foreach (var route in new[] { "//evil.test", "https://evil.test", "javascript:alert(1)", "/dashboard", "/about?x=1", "/about#fake", "/about\n", "/../admin", "/%2fadmin" })
            Check(!new AboutCtaRouteAttribute().IsValid(route), "Unsafe CTA rejected.");
        foreach (var model in new object[] { new AboutTimelineItemEditModel { Year = 999, Description = "x" }, new AboutTimelineItemEditModel { Year = 10000, Description = "x" }, new AboutTimelineItemEditModel { Year = 2026, Description = new string('x',601) }, new AboutMissionPointEditModel { Description = new string('x',301) } })
            Check(!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true), "Collection field limits enforced.");
    }
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static void WireEditor(object editor, object model, IAboutCmsService service)
    {
        SetField(editor, "_model", model); SetProperty(editor, "AboutService", service);
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
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public ClaimsPrincipal User { get; set; } = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
    }
    private sealed class TestFile(byte[] bytes) : IBrowserFile
    {
        public string Name => "about-test.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes, false);
    }
}
