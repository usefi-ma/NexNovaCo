using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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

internal static class DetailTemplateChecks
{
    private const string Site = "https://templates.example.invalid";
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword(), publicBaseUrl: Site);
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
        var logger = new TestLogger();
        var service = new DetailTemplateContentService(factory, auth, options, logger);
        var projects = new ProjectContentService(factory, auth, options, NullLogger<ProjectContentService>.Instance, app.Services.GetRequiredService<ProjectCatalog>());
        var members = new MemberContentService(factory, auth, options, NullLogger<MemberContentService>.Instance, app.Services.GetRequiredService<MemberCatalog>());
        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "detail-viewer@example.invalid", UserName = "detail-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);

        string prior;
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(!db.Database.HasPendingModelChanges(), "Migration and model agree.");
            prior = await PriorSnapshotAsync(db);
            var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(migrations[^1].EndsWith("AddDetailTemplateSettings"), "New additive migration applied.");
            // Exercise upgrade of existing data, not just fresh EnsureCreated schema.
            await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
            Check(await PriorSnapshotAsync(db) == prior, "Downgrade of only new tables preserves all prior rows.");
            await db.Database.MigrateAsync();
            await DetailTemplateInitializer.InitializeAsync(db);
            await DetailTemplateInitializer.InitializeAsync(db);
            Check(await db.ProjectDetailTemplateSettings.CountAsync() == 1 && await db.MemberDetailTemplateSettings.CountAsync() == 1, "Singleton seed once.");
            Check(await PriorSnapshotAsync(db) == prior, "Additive upgrade/initialization preserves prior CMS rows.");
        }
        var sitemapBefore = await client.GetStringAsync("/sitemap.xml");
        var projectRecords = await projects.GetAsync();
        var memberRecords = await members.GetAsync();
        var kinds = new[] { "Project", "Member" };
        var editorTypes = new[] { typeof(ProjectDetailTemplateEditor), typeof(MemberDetailTemplateEditor) };
        for (var i = 0; i < kinds.Length; i++)
        {
            var kind = kinds[i]; var area = i == 0 ? "projects" : "team";
            var route = $"/dashboard/content/{area}/detail-template";
            var table = kind + "DetailTemplateSettings";
            var model = (await CallAsync(service, "Get" + kind + "ForEditAsync", CancellationToken.None))!;
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            Check(JsonSerializer.Serialize(model) == JsonSerializer.Serialize(approved), kind + " exact approved initialization.");
            var challenge = await client.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous route challenged.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Viewer route denied.");
            for (var refresh = 0; refresh < 2; refresh++)
            {
                var html = await adminClient.GetStringAsync(route);
                Check(html.Contains(kind + " Detail Template") && html.Contains($"href=\"/dashboard/content/shared-{area}\""), "Admin direct/refresh and canonical manager link.");
                Check(Regex.IsMatch(html, "<a[^>]*class=\"[^\"]*active[^\"]*\"[^>]*href=\"" + route + "\"|<a[^>]*href=\"" + route + "\"[^>]*class=\"[^\"]*active[^\"]*\""), "Active sidebar rendered.");
            }
            var editor = Activator.CreateInstance(editorTypes[i])!;
            SetField(editor, "_model", model);
            SetProperty(editor, "Templates", service);
            SetProperty(editor, "Catalog", i == 0 ? projects : members);
            var nav = new TestNavigation(); SetProperty(editor, "Navigation", nav);
            SetProperty(editor, "Logger", Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(editorTypes[i])));
            ((EditorSnapshot)GetField(editor, "_snapshot")!).Capture((string[])GetProperty(editor, "CurrentValues")!);
            Check(!Dirty(editor), "Initial editor clean.");
            await CallAsync(editor, "OpenSampleAsync");
            Check(nav.Last == "/" + (i == 0 ? projectRecords[0].DetailHref : memberRecords[0].ProfileHref), "Sample uses current canonical entity.");
            foreach (var property in model.GetType().GetProperties())
            {
                var old = property.GetValue(model);
                foreach (var invalid in new[] { "   ", new string('x', property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength + 1), "<b>HTML</b>", "line\nfeed" })
                {
                    property.SetValue(model, invalid);
                    await RejectAsync<ValidationException>(() => SaveAsync(service, kind, model), "Every label validated on server.");
                }
                property.SetValue(model, old);
            }
            // Every visible field receives a unique value; all entities must inherit it.
            foreach (var property in model.GetType().GetProperties()) property.SetValue(model, "Edited " + property.Name);
            Check(Dirty(editor), "Edit becomes dirty.");
            await DdlAsync(factory, $"CREATE TRIGGER BlockDetailSave BEFORE UPDATE ON {table} BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, "Failed save retains input/dirty state.");
            await DdlAsync(factory, "DROP TRIGGER BlockDetailSave;");
            var first = model.GetType().GetProperties()[0]; var firstValue = first.GetValue(model);
            first.SetValue(model, " "); await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, "Validation failure stays dirty.");
            first.SetValue(model, firstValue); await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Valid save marks clean.");
            var slugs = i == 0 ? projectRecords.Select(x => x.Slug) : memberRecords.Select(x => x.Slug);
            foreach (var slug in slugs)
            {
                var html = await client.GetStringAsync($"/{area}/{slug}");
                foreach (var property in model.GetType().GetProperties()) Check(html.Contains((string)property.GetValue(model)!), "Every template field reaches every detail page.");
                var name = i == 0 ? projectRecords.Single(x => x.Slug == slug).Name : memberRecords.Single(x => x.Slug == slug).Name;
                var description = i == 0 ? projectRecords.Single(x => x.Slug == slug).Description : memberRecords.Single(x => x.Slug == slug).Introduction;
                Check(WebUtility.HtmlDecode(html).Contains(name) && WebUtility.HtmlDecode(html).Contains(description), "Entity title/description remain canonical.");
                Check(html.Contains($"<link rel=\"canonical\" href=\"{Site}/{area}/{slug}\"") && html.Contains("property=\"og:image\"") && html.Contains("property=\"og:url\""), "SEO uses real slug and social metadata.");
                var json = Regex.Match(html, "<script type=\"application/ld(?:\\+|&#x2B;)json\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
                using var doc = JsonDocument.Parse(json);
                var crumbs = doc.RootElement.GetProperty("@graph").EnumerateArray().Single(x => x.GetProperty("@type").GetString() == "BreadcrumbList").GetProperty("itemListElement");
                Check(crumbs[0].GetProperty("name").GetString() == "Edited BreadcrumbHomeLabel" && crumbs[1].GetProperty("name").GetString() == "Edited BreadcrumbSectionLabel" && crumbs[2].GetProperty("name").GetString() == name, "JSON-LD names match visual breadcrumbs.");
                Check(crumbs[0].GetProperty("item").GetString() == Site + "/" && crumbs[1].GetProperty("item").GetString() == Site + "/" + area && crumbs[2].GetProperty("item").GetString() == Site + "/" + area + "/" + slug, "Breadcrumb URLs remain fixed/entity-owned.");
                if (i == 0) Check(html.Contains($"href=\"projects/{slug}#inner-project\""), "Hero anchor remains code-controlled.");
            }
            Check(await client.GetStringAsync("/sitemap.xml") == sitemapBefore, "Template edit does not affect sitemap.");
            await using (var restart = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: Site))
            {
                using var restarted = restart.NewClient();
                Check((await restarted.GetStringAsync($"/{area}/{slugs.First()}")).Contains("Edited BreadcrumbSectionLabel"), "Host restart preserves Admin edits.");
            }
            Check(JsonSerializer.Serialize(await CallAsync(service, "Read" + kind + "Async", CancellationToken.None)) == JsonSerializer.Serialize(model), "Initializer did not overwrite edits.");
            await DdlAsync(factory, $"ALTER TABLE {table} RENAME TO {table}_Unavailable;");
            try
            {
                Check(JsonSerializer.Serialize(await CallAsync(service, "Read" + kind + "Async", CancellationToken.None)) == JsonSerializer.Serialize(approved), "Missing-table read falls back to exact approved strings.");
                Check((await client.GetAsync($"/{area}/{slugs.First()}")).IsSuccessStatusCode, "Public detail survives template read failure.");
            }
            finally { await DdlAsync(factory, $"ALTER TABLE {table}_Unavailable RENAME TO {table};"); }
            Check(JsonSerializer.Serialize(await CallAsync(service, "Read" + kind + "Async", CancellationToken.None)) == JsonSerializer.Serialize(model), "Fallback did not overwrite edited values.");
            first.SetValue(model, "  Normal & Unicode café  ");
            await SaveAsync(service, kind, model);
            var normalized = await CallAsync(service, "Get" + kind + "ForEditAsync", CancellationToken.None);
            Check((string)first.GetValue(normalized)! == "Normal & Unicode café", "Surrounding whitespace trimmed, normal text preserved.");
            await SaveAsync(service, kind, approved);
        }
        Check(logger.Errors >= 2, "Read fallback logged.");
        await using (var db = await factory.CreateDbContextAsync()) Check(await PriorSnapshotAsync(db) == prior, "Template workflow leaves all prior tables untouched.");

        // Shared entity CRUD still controls the detail, and empty collections stay empty.
        foreach (var area in new[] { "projects", "team" }) Check((await adminClient.GetAsync("/dashboard/content/shared-" + area)).IsSuccessStatusCode, "Canonical manager smoke.");
        var projectRow = (await projects.ListAsync())[0]; var projectEdit = await projects.GetForEditAsync(projectRow.Id);
        projectEdit.Name = "Isolated Project Name"; await projects.UpdateAsync(projectRow.Id, projectEdit);
        Check((await client.GetStringAsync("/projects/" + projectEdit.Slug)).Contains(projectEdit.Name), "Shared Project edit reaches detail.");
        var memberRow = (await members.ListAsync())[0]; var memberEdit = await members.GetForEditAsync(memberRow.Id);
        memberEdit.Name = "Isolated Member Name"; await members.UpdateAsync(memberRow.Id, memberEdit);
        Check((await client.GetStringAsync("/team/" + memberEdit.Slug)).Contains(memberEdit.Name), "Shared Member edit reaches detail.");
        await projects.DeleteAsync(projectRow.Id); await members.DeleteAsync(memberRow.Id);
        var deletedSitemap = await client.GetStringAsync("/sitemap.xml");
        Check(!deletedSitemap.Contains("/projects/" + projectEdit.Slug) && !deletedSitemap.Contains("/team/" + memberEdit.Slug), "Deleted slugs leave sitemap.");
        Check((await client.GetAsync("/projects/" + projectEdit.Slug)).StatusCode == HttpStatusCode.NotFound && (await client.GetAsync("/team/" + memberEdit.Slug)).StatusCode == HttpStatusCode.NotFound, "Deleted details remain 404.");
        foreach (var row in await projects.ListAsync()) await projects.DeleteAsync(row.Id);
        foreach (var row in await members.ListAsync()) await members.DeleteAsync(row.Id);
        await using (var restart = new AuthFactory(app.Password, databasePath: app.DatabasePath))
        { using var c = restart.NewClient(); Check((await c.GetAsync("/projects")).IsSuccessStatusCode && (await c.GetAsync("/team")).IsSuccessStatusCode, "Empty public listings safe after restart."); }
        Check((await projects.ListAsync()).Count == 0 && (await members.ListAsync()).Count == 0, "Restart never reseeds deleted entities.");
        for (var i = 0; i < 2; i++)
        {
            var editor = Activator.CreateInstance(editorTypes[i])!; var nav = new TestNavigation();
            SetProperty(editor, "Catalog", i == 0 ? projects : members); SetProperty(editor, "Navigation", nav);
            await CallAsync(editor, "OpenSampleAsync");
            Check(nav.Last == (i == 0 ? "/projects" : "/team"), "Empty sample falls back to listing.");
            Check((await adminClient.GetAsync($"/dashboard/content/{(i == 0 ? "projects" : "team")}/detail-template")).IsSuccessStatusCode, "Empty entities do not break template editor.");
        }
        var operations = new Func<Task>[] { () => service.GetProjectForEditAsync(), () => service.GetMemberForEditAsync(), () => service.SaveProjectAsync(ProjectDetailTemplateEditModel.Approved()), () => service.SaveMemberAsync(MemberDetailTemplateEditModel.Approved()) };
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), await signIn.CreateUserPrincipalAsync(viewerUser) })
        { auth.User = identity; foreach (var op in operations) await RejectAsync<UnauthorizedAccessException>(op, "Admin-only reads/writes enforced."); }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin"); foreach (var op in operations) await RejectAsync<UnauthorizedAccessException>(op, "Live role revocation enforced.");
        await users.AddToRoleAsync(admin, "Admin"); await users.UpdateSecurityStampAsync(admin);
        foreach (var op in operations) await RejectAsync<UnauthorizedAccessException>(op, "Security stamp revocation enforced.");
    }

    private static async Task<string> PriorSnapshotAsync(ApplicationDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        var connection = db.Database.GetDbConnection();
        using var tables = connection.CreateCommand();
        tables.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%' AND name NOT LIKE '%DetailTemplateSettings%' ORDER BY name";
        var names = new List<string>();
        await using (var reader = await tables.ExecuteReaderAsync()) while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        var data = new List<string>();
        foreach (var name in names)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + name.Replace("\"", "\"\"") + "\" ORDER BY rowid";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) { var row = new object[reader.FieldCount]; reader.GetValues(row); data.Add(name + JsonSerializer.Serialize(row)); }
        }
        return string.Join("\n", data);
    }
    private static async Task DdlAsync(IDbContextFactory<ApplicationDbContext> factory, string sql)
    {
        await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql; await command.ExecuteNonQueryAsync();
    }
    private static Task SaveAsync(object service, string kind, object model) => CallAsync(service, "Save" + kind + "Async", model, CancellationToken.None);
    private static void SetField(object o, string name, object? value) => o.GetType().GetField(name, Flags)!.SetValue(o, value);
    private static object? GetField(object o, string name) => o.GetType().GetField(name, Flags)!.GetValue(o);
    private static void SetProperty(object o, string name, object? value) => o.GetType().GetProperty(name, Flags)!.SetValue(o, value);
    private static object? GetProperty(object o, string name) => o.GetType().GetProperty(name, Flags)!.GetValue(o);
    private static bool Dirty(object o) => (bool)GetProperty(o, "IsDirty")!;
    private static async Task<object?> CallAsync(object o, string method, params object[] args)
    {
        var target = o.GetType().GetMethods(Flags).Single(x => x.Name == method && x.GetParameters().Length == args.Length);
        var task = (Task)target.Invoke(o, args)!; await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }
    private static async Task RejectAsync<T>(Func<Task> action, string label) where T : Exception
    { try { await action(); } catch (T) { Check(true, label); return; } Check(false, label); }
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    { public ClaimsPrincipal User { get; set; } = user; public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User)); }
    private sealed class TestNavigation : NavigationManager
    {
        public string? Last;
        public TestNavigation() => Initialize("https://localhost/", "https://localhost/dashboard");
        protected override void NavigateToCore(string uri, bool forceLoad) => Last = uri;
    }
    private sealed class TestLogger : ILogger<DetailTemplateContentService>
    {
        public int Errors;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { if (level == LogLevel.Error) Errors++; }
    }
}
