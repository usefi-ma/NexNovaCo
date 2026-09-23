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

internal static class ContactPageCmsChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var client = app.NewClient();
        using var adminClient = app.NewClient();
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
        var service = new ContactPageCmsService(factory, auth, options, media, logger);
        var sections = new[] { "Hero", "Info", "Form", "Map" };
        var types = new[] { typeof(ContactHeroEditor), typeof(ContactInfoEditor), typeof(ContactFormEditor), typeof(ContactMapEditor) };
        Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(ContactPageDefaults.Content), "Fresh Contact matches approved content exactly.");
        await RestartAsync(app);
        Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(ContactPageDefaults.Content), "Fresh restart stays exact.");
        await MigrationAsync(factory);
        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "contact-viewer@example.invalid", UserName = "contact-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);
        foreach (var route in sections.Select(x => "/dashboard/content/contact/" + x.ToLowerInvariant()).Prepend("/dashboard/content/contact"))
        {
            var challenge = await client.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous challenges: " + route);
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Viewer denied: " + route);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var response = await adminClient.GetAsync(route);
                Check(response.IsSuccessStatusCode || route == "/dashboard/content/contact" && response.StatusCode == HttpStatusCode.Redirect &&
                    response.Headers.Location!.ToString().EndsWith("/dashboard/content/contact/hero"), "Admin direct/refresh: " + route);
            }
        }
        string prior;
        await using (var db = await factory.CreateDbContextAsync()) prior = await SnapshotPriorTablesAsync(db);
        for (var i = 0; i < sections.Length; i++)
        {
            var part = sections[i];
            var model = (await CallAsync(service, "Get" + part + "ForEditAsync", CancellationToken.None))!;
            var editor = Activator.CreateInstance(types[i])!;
            WireEditor(editor, model, service);
            Check(!Dirty(editor), part + " initially clean.");
            foreach (var property in model.GetType().GetProperties())
            {
                if (property.Name == "ImagePath") continue;
                property.SetValue(model, property.Name switch
                {
                    "Email" => "isolated@example.invalid",
                    "CtaHref" => "/contact#Contact",
                    "EmbedUrl" => "https://www.google.com/maps/embed?pb=isolated",
                    "Phone" => "+1 (555) 010-0200",
                    _ => part + " edited " + property.Name
                });
            }
            Check(Dirty(editor), part + " edit dirty.");
            var table = part == "Form" ? "ContactFormSettings" : part == "Info" ? "SiteContactSettings" : "ContactPageSettings";
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockContactSave BEFORE UPDATE ON " + table + " BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, part + " failure retains dirty edits.");
            await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockContactSave;");
            var title = model.GetType().GetProperty(part == "Hero" ? "Title" : "Heading")!;
            var savedTitle = title.GetValue(model);
            title.SetValue(model, "");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, part + " required validation preserves edits.");
            title.SetValue(model, savedTitle);
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, part + " successful handler clears dirty.");
            var html = WebUtility.HtmlDecode(await client.GetStringAsync("/contact"));
            foreach (var property in model.GetType().GetProperties())
                if (property.Name != "InvalidMessage")
                    Check(html.Contains((string)property.GetValue(model)!), part + " public field " + property.Name);
            if (model is ContactFormEditModel editedForm)
                Check((await service.ReadPublicAsync()).Form.InvalidMessage == editedForm.InvalidMessage, "Edited invalid message reaches public form content.");
            if (part == "Info")
            {
                var home = await client.GetStringAsync("/");
                var footer = home[home.IndexOf("<footer", StringComparison.Ordinal)..];
                Check(footer.Contains("mailto:isolated@example.invalid") && !footer.Contains("mailto:someone@example.com"), "Footer shares canonical email.");
                await using var db = await factory.CreateDbContextAsync();
                Check((await db.SiteContactSettings.SingleAsync()).Email == "isolated@example.invalid", "Business data stored in only canonical source.");
            }
            await RestartAsync(app);
            Check(JsonSerializer.Serialize(await CallAsync(service, "Get" + part + "ForEditAsync", CancellationToken.None)) == JsonSerializer.Serialize(model), part + " restart persistence.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + part + "Async", approved, CancellationToken.None);
        }
        await ImageAsync(service, media, factory, client);
        await FallbackAsync(service, factory, client);
        await PlainTextAndAtomicityAsync(service, factory, client);
        Check(logger.Errors >= 3, "Fallback errors logged.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await SnapshotPriorTablesAsync(db) == prior, "All prior Identity/CMS rows/timestamps unchanged by Contact workflows.");
        foreach (var route in new[] { "/", "/team", "/team/emilyjohnson", "/dashboard/content/team/hero", "/dashboard/content/shared-team" })
            Check((await adminClient.GetAsync(route)).IsSuccessStatusCode, "Prior CMS/public smoke: " + route);
        var operations = sections.SelectMany(part => new Func<Task>[] {
            async () => { await CallAsync(service, "Get" + part + "ForEditAsync", CancellationToken.None); },
            async () => { var model = types[Array.IndexOf(sections, part)].GetField("_model", Flags)!.FieldType.GetMethod("Approved")!.Invoke(null, null)!;
                await CallAsync(service, "Save" + part + "Async", model, CancellationToken.None); }
        }).ToArray();
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), await signIn.CreateUserPrincipalAsync(viewerUser) })
        {
            auth.User = identity;
            foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Anonymous/viewer service denied.");
        }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin");
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Revoked role denied.");
        await users.AddToRoleAsync(admin, "Admin");
        await users.UpdateSecurityStampAsync(admin);
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Stale stamp denied.");
        Validation();
    }

    private static async Task PlainTextAndAtomicityAsync(ContactPageCmsService service, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var form = ContactFormEditModel.Approved();
        form.Heading = "<script>contactContentProbe()</script>";
        await service.SaveFormAsync(form);
        var html = await client.GetStringAsync("/contact");
        Check(html.Contains("&lt;script&gt;contactContentProbe()&lt;/script&gt;") && !html.Contains(form.Heading), "CMS text is encoded, never interpreted as arbitrary HTML.");
        await service.SaveFormAsync(ContactFormEditModel.Approved());
        var info = ContactInfoEditModel.Approved();
        info.Heading = "Must roll back";
        info.Email = "rollback@example.invalid";
        await using (var db = await factory.CreateDbContextAsync())
        {
            var before = await SnapshotContactAsync(db);
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER BlockCanonicalContact BEFORE UPDATE ON SiteContactSettings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            try { await RejectAsync<DbUpdateException>(() => service.SaveInfoAsync(info), "Canonical-contact failure is reported."); }
            finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockCanonicalContact;"); }
            Check(await SnapshotContactAsync(db) == before, "Contact heading and canonical info roll back atomically on failure.");
            await db.Database.ExecuteSqlRawAsync("UPDATE ContactPageSettings SET MapEmbedUrl = 'javascript:alert(1)' WHERE Id = 1;");
            var beforeFallback = await SnapshotContactAsync(db);
            Check((await service.ReadPublicAsync()).Map == ContactPageDefaults.Content.Map, "Invalid stored map falls back to approved safe embed.");
            Check(await SnapshotContactAsync(db) == beforeFallback, "Invalid-data fallback does not repair/write the database.");
        }
        await service.SaveMapAsync(ContactMapEditModel.Approved());
    }

    private static async Task ImageAsync(ContactPageCmsService service, IMediaStorageService media, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.ContactHeroDefault));
        var model = await service.GetHeroForEditAsync();
        var editor = new ContactHeroEditor(); WireEditor(editor, model, service);
        var field = new CmsImageField();
        SetProperty(field, "Media", media); SetProperty(field, "Value", model.ImagePath); SetProperty(field, "Kind", MediaKind.ContactHero);
        var selected = await media.ReadAsync(new TestFile(bytes), MediaKind.ContactHero);
        SetField(field, "_pending", selected); SetField(field, "_selectedName", selected.FileName);
        SetField(editor, "_imageField", field);
        Check(Dirty(editor), "Hero image-only selection dirty.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER BlockContactImage BEFORE UPDATE ON ContactPageSettings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
        await CallAsync(editor, "SaveAsync");
        Check(Dirty(editor) && field.HasPendingSelection, "Failed save retains selected image.");
        var path = (string)GetField(field, "_storedPath")!;
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockContactImage;");
        await CallAsync(editor, "SaveAsync");
        Check(!Dirty(editor) && !field.HasPendingSelection && MediaPolicy.IsGenerated(path, MediaKind.ContactHero), "Retry accepts uploaded image.");
        var image = await client.GetAsync("/" + path);
        Check(image.IsSuccessStatusCode && image.Content.Headers.ContentType!.MediaType == "image/jpeg" && image.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "Safe raster served.");
        Check((await client.GetStringAsync("/contact")).Contains(path), "Public image from SQLite.");
        await service.SaveHeroAsync(ContactHeroEditModel.Approved());
        field.Dispose();
    }

    private static async Task FallbackAsync(ContactPageCmsService service, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var hero = ContactHeroEditModel.Approved(); hero.Title = "Stored Hero";
        var info = ContactInfoEditModel.Approved(); info.Email = "stored@example.invalid"; info.Heading = "Stored Info";
        var form = ContactFormEditModel.Approved(); form.Heading = "Stored Form";
        var map = ContactMapEditModel.Approved(); map.Title = "Stored Map";
        await service.SaveHeroAsync(hero); await service.SaveInfoAsync(info); await service.SaveFormAsync(form); await service.SaveMapAsync(map);
        var stored = await service.ReadPublicAsync();
        foreach (var table in new[] { "ContactPageSettings", "ContactFormSettings", "SiteContactSettings" })
        {
            await using var db = await factory.CreateDbContextAsync();
            var before = await SnapshotContactAsync(db);
            await ExecuteFixtureDdlAsync(db, "ALTER TABLE " + table + " RENAME TO IsolatedUnavailable;");
            try
            {
                var result = await service.ReadPublicAsync();
                var defaults = ContactPageDefaults.Content;
                var expected = table switch
                {
                    "ContactPageSettings" => stored with { Hero = defaults.Hero, Map = defaults.Map, Info = stored.Info with { Heading = defaults.Info.Heading } },
                    "ContactFormSettings" => stored with { Form = defaults.Form },
                    _ => stored with { Info = defaults.Info with { Heading = stored.Info.Heading } }
                };
                Check(JsonSerializer.Serialize(result) == JsonSerializer.Serialize(expected), table + " slice-only fallback.");
                Check((await client.GetAsync("/contact")).IsSuccessStatusCode && (await client.GetAsync("/")).IsSuccessStatusCode, "Public page/Footer safe on missing settings.");
            }
            finally { await ExecuteFixtureDdlAsync(db, "ALTER TABLE IsolatedUnavailable RENAME TO " + table + ";"); }
            Check(await SnapshotContactAsync(db) == before, "Fallback does not mutate rows or timestamps.");
        }
        await service.SaveHeroAsync(ContactHeroEditModel.Approved()); await service.SaveInfoAsync(ContactInfoEditModel.Approved());
        await service.SaveFormAsync(ContactFormEditModel.Approved()); await service.SaveMapAsync(ContactMapEditModel.Approved());
    }
    private static async Task<string> SnapshotContactAsync(ApplicationDbContext db) => JsonSerializer.Serialize(new {
        Page = await db.ContactPageSettings.AsNoTracking().SingleAsync(), Form = await db.ContactFormSettings.AsNoTracking().SingleAsync(), Site = await db.SiteContactSettings.AsNoTracking().SingleAsync() });

    private static Task ExecuteFixtureDdlAsync(ApplicationDbContext db, string fixtureSql) => db.Database.ExecuteSqlRawAsync(fixtureSql);
    private static async Task MigrationAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Check(migrations.Length == 21, "One additive Contact migration.");
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        var before = await SnapshotPriorTablesAsync(db);
        await db.Database.MigrateAsync();
        await ContactPageInitializer.InitializeAsync(db);
        Check(await SnapshotPriorTablesAsync(db) == before, "Upgrade preserves every prior row/timestamp.");
        Check(!db.Database.HasPendingModelChanges(), "Migration matches runtime model.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[^1]], db.Database.ProviderName!);
        Check(migration.UpOperations.Count == 3 && migration.UpOperations.All(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation), "Exactly three additive tables.");
    }
    private static async Task<string> SnapshotPriorTablesAsync(ApplicationDbContext db)
    {
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT IN ('ContactPageSettings', 'ContactFormSettings', 'SiteContactSettings') AND name NOT LIKE '__EF%' AND name <> 'sqlite_sequence' ORDER BY name").ToArrayAsync();
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
        Check((await client.GetAsync("/contact")).IsSuccessStatusCode, "Separate host restart on isolated existing database.");
    }
    private static void Validation()
    {
        foreach (var url in new[] { "http://www.google.com/maps/embed?pb=x", "javascript:alert(1)", "//www.google.com/maps/embed?pb=x", "https://www.google.com.evil.test/maps/embed?pb=x", "https://evil.test/maps/embed?pb=x", "https://www.google.com/maps?pb=x", "https://www.google.com/maps/embed", "https://www.google.com:8443/maps/embed?pb=x", "https://user@www.google.com/maps/embed?pb=x", "https://www.google.com/maps/embed?pb=x#fragment", "<iframe>bad</iframe>", "https://www.google.com/maps/embed?pb=x\n" })
            Check(!new ContactMapUrlAttribute().IsValid(url), "Unsafe map URL rejected.");
        Check(new ContactMapUrlAttribute().IsValid(ContactPageDefaults.Content.Map.EmbedUrl.AbsoluteUri), "Approved embed allowed.");
        foreach (var url in new[] { "/", "contact#Contact", "/contact#Contact", "/projects/nexconnect" })
            Check(new ContactPageCtaRouteAttribute().IsValid(url), "Safe CTA allowed.");
        foreach (var url in new[] { "https://evil.test", "//evil.test", "javascript:alert(1)", "/dashboard", "/contact?x=1", "/contact#fake", "/../admin" })
            Check(!new ContactPageCtaRouteAttribute().IsValid(url), "Unsafe CTA rejected.");
        foreach (var type in new[] { typeof(ContactHeroEditModel), typeof(ContactInfoEditModel), typeof(ContactFormEditModel), typeof(ContactMapEditModel) })
        foreach (var property in type.GetProperties())
        {
            var model = type.GetMethod("Approved")!.Invoke(null, null)!;
            property.SetValue(model, " ");
            Check(!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true), "Required: " + property.Name);
            property.SetValue(model, new string('x', property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength + 1));
            Check(!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true), "Max length: " + property.Name);
        }
        var invalid = ContactInfoEditModel.Approved(); invalid.Email = "not-an-email";
        Check(!Validator.TryValidateObject(invalid, new ValidationContext(invalid), new List<ValidationResult>(), true), "Invalid business email rejected.");
    }
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static void WireEditor(object editor, object model, IContactPageCmsService service)
    {
        SetField(editor, "_model", model); SetProperty(editor, "ContactPageService", service);
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
    private sealed class TestLogger : ILogger<ContactPageCmsService>
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
        public string Name => "contact-test.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes, false);
    }
}
