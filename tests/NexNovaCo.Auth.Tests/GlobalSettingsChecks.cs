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

internal static class GlobalSettingsChecks
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
        var logger = new TestLogger<GlobalSettingsService>();
        var navLogger = new TestLogger<NavigationContentService>();
        var socialLogger = new TestLogger<SocialLinkContentService>();
        var service = new GlobalSettingsService(factory, auth, options, media, logger);
        var nav = new NavigationContentService(factory, auth, options, navLogger);
        var social = new SocialLinkContentService(factory, auth, options, socialLogger);
        var contact = new ContactPageCmsService(factory, auth, options, media, NullLogger<ContactPageCmsService>.Instance);
        await MigrationAsync(factory);
        Check(JsonSerializer.Serialize(await service.ReadSiteIdentityAsync()) == JsonSerializer.Serialize(SiteIdentityEditModel.Approved()), "Exact approved branding.");
        Check(JsonSerializer.Serialize(await service.ReadFooterAsync()) == JsonSerializer.Serialize(FooterEditModel.Approved()), "Exact approved Footer.");
        Check((await nav.GetAsync()).SequenceEqual(GlobalSiteDefaults.Navigation), "Exact approved navigation order/content.");
        Check((await social.GetAsync()).SequenceEqual(GlobalSiteDefaults.SocialLinks), "Exact approved global icons, no invented URLs.");
        string prior;
        await using (var db = await factory.CreateDbContextAsync()) prior = await SnapshotPriorTablesAsync(db);
        await RestartAsync(app);
        Check((await nav.ListAsync()).Count == 6 && (await social.ListAsync()).Count == 3, "Restart creates no duplicates.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await db.NavigationInitializationStates.CountAsync() == 1 && await db.SocialLinkInitializationStates.CountAsync() == 1 &&
                await db.SiteContactSettings.CountAsync() == 1, "Persistent initialization markers and existing contact singleton.");

        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "global-viewer@example.invalid", UserName = "global-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);
        var routes = new[] { "", "/site", "/navigation", "/navigation/new", "/navigation/1", "/footer", "/social", "/social/new", "/social/1", "/contact" };
        foreach (var suffix in routes)
        {
            var route = "/dashboard/settings" + suffix;
            var challenge = await client.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous challenged: " + route);
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Viewer denied: " + route);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var response = await adminClient.GetAsync(route);
                Check(response.IsSuccessStatusCode || suffix == "" && response.StatusCode == HttpStatusCode.Redirect &&
                    response.Headers.Location!.ToString().EndsWith("/dashboard/settings/site"), "Admin direct/refresh: " + route);
            }
        }
        // Include the viewer in the preservation baseline; subsequent CMS work must not change Identity.
        await using (var db = await factory.CreateDbContextAsync()) prior = await SnapshotPriorTablesAsync(db);
        await SingletonEditorsAsync(service, contact, factory, client, app);
        await CollectionEditorsAsync(nav, social, factory);
        await ImageAsync(service, media, factory, client);
        await CollectionsAsync(nav, social, factory, client, app);
        await FallbackAsync(service, nav, social, factory, client);
        Check(logger.Errors >= 2 && navLogger.Errors >= 1 && socialLogger.Errors >= 1, "Read fallbacks log errors.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(await SnapshotPriorTablesAsync(db) == prior, "Every prior Identity/CMS row and timestamp remains intact, except deliberately edited canonical contact.");
        foreach (var route in new[] { "/", "/about", "/services", "/projects", "/projects/nexconnect", "/team", "/contact",
            "/dashboard/content/home/hero", "/dashboard/content/about/hero", "/dashboard/content/services/hero",
            "/dashboard/content/projects/hero", "/dashboard/content/team/hero", "/dashboard/content/contact/info", "/dashboard/content/shared-team" })
            Check((await adminClient.GetAsync(route)).IsSuccessStatusCode, "Prior public/CMS route: " + route);
        await AuthorizationAsync(auth, principal, await signIn.CreateUserPrincipalAsync(viewerUser), users, admin, service, nav, social, contact, media);
        Validation();
    }

    private static async Task SingletonEditorsAsync(GlobalSettingsService service, ContactPageCmsService contact,
        IDbContextFactory<ApplicationDbContext> factory, HttpClient client, AuthFactory app)
    {
        var site = await service.GetSiteIdentityForEditAsync();
        var footer = await service.GetFooterForEditAsync();
        var info = await contact.GetSiteContactForEditAsync();
        var editors = new object[] { new GlobalSiteIdentityEditor(), new GlobalFooterEditor(), new GlobalSiteContactEditor() };
        var models = new object[] { site, footer, info };
        for (var i = 0; i < editors.Length; i++)
        {
            var editor = editors[i]; var model = models[i];
            WireEditor(editor, model, i == 2 ? contact : service, i == 2 ? "ContactPageService" : "GlobalSettings");
            Check(!Dirty(editor), "Singleton initially clean.");
            foreach (var property in model.GetType().GetProperties())
            {
                if (property.Name == "ImagePath" || property.PropertyType == typeof(bool)) continue;
                property.SetValue(model, property.Name switch {
                    "Email" => "global-qa@example.invalid", "Phone" => "+1 555 010 1234", _ => "Edited " + property.Name
                });
            }
            Check(Dirty(editor), "Singleton text edits tracked.");
            var table = i switch { 0 => "SiteIdentitySettings", 1 => "FooterSettings", _ => "SiteContactSettings" };
            await using (var db = await factory.CreateDbContextAsync())
                await FixtureSqlAsync(db, "CREATE TRIGGER BlockGlobalSave BEFORE UPDATE ON " + table + " BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, "Failed save preserves input/dirty state.");
            await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockGlobalSave;");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Save handler clears dirty state only after persistence.");
        }
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        Check(html.Contains("Edited SiteName home") && html.Contains("<h2 class=\"h3\">Edited SiteName</h2>"), "Canonical name in Header and Footer.");
        Check(html.Contains("Edited Description") && html.Contains("Edited Copyright") &&
            html.Contains("Edited NewsletterHeading") && html.Contains("Edited NewsletterPlaceholder") && html.Contains("Edited NewsletterSubmitLabel"), "Footer fields render.");
        Check(html.Contains("Newsletter signup is not available yet.") && html.Contains("aria-disabled=\"true\""), "Newsletter remains honest and unavailable.");
        Check(html.Contains("mailto:global-qa@example.invalid"), "Footer uses canonical contact email.");
        Check(WebUtility.HtmlDecode(await client.GetStringAsync("/contact")).Contains("global-qa@example.invalid"), "Contact page shares email.");
        await using (var db = await factory.CreateDbContextAsync())
            Check((await db.ContactPageSettings.SingleAsync()).InfoHeading == ContactPageDefaults.Content.Info.Heading, "Global contact does not alter page-specific heading.");
        footer.NewsletterVisible = false;
        Check(Dirty(editors[1]), "Newsletter checkbox tracked.");
        await CallAsync(editors[1], "SaveAsync");
        Check(!(await client.GetStringAsync("/")).Contains("newsletter-notice"), "Hidden newsletter not rendered.");
        await RestartAsync(app);
        Check(JsonSerializer.Serialize(await service.GetSiteIdentityForEditAsync()) == JsonSerializer.Serialize(site), "Brand restart persistence.");
        Check(JsonSerializer.Serialize(await service.GetFooterForEditAsync()) == JsonSerializer.Serialize(footer), "Footer restart persistence.");
        Check(JsonSerializer.Serialize(await contact.GetSiteContactForEditAsync()) == JsonSerializer.Serialize(info), "Canonical contact restart persistence.");
        site.SiteName = "<script>probe()</script>";
        await service.SaveSiteIdentityAsync(site);
        html = await client.GetStringAsync("/");
        Check(html.Contains("&lt;script&gt;probe()&lt;/script&gt;") && !html.Contains(site.SiteName), "Brand text encoded, never HTML.");
        await service.SaveSiteIdentityAsync(SiteIdentityEditModel.Approved());
        await service.SaveFooterAsync(FooterEditModel.Approved());
        var approved = ContactPageDefaults.Content.Info;
        await contact.SaveSiteContactAsync(new() { Phone = approved.Phone, Email = approved.Email, Address = approved.Address });
    }

    private static async Task CollectionEditorsAsync(NavigationContentService nav, SocialLinkContentService social,
        IDbContextFactory<ApplicationDbContext> factory)
    {
        var navigation = await nav.GetForEditAsync(1);
        var socialModel = await social.GetForEditAsync(2);
        var editors = new object[] { new NavigationEditor(), new SocialLinkEditor() };
        var models = new object[] { navigation, socialModel };
        for (var i = 0; i < 2; i++)
        {
            var editor = editors[i]; var model = models[i];
            WireEditor(editor, model, i == 0 ? nav : social, "ContentService");
            SetField(editor, "_editingId", i == 0 ? 1 : 2);
            SetProperty(editor, "Id", i == 0 ? 1 : 2);
            Check(!Dirty(editor), "Existing link editor starts clean.");
            if (i == 0) navigation.Label = "Start"; else socialModel.Url = "https://www.linkedin.com/company/example";
            Check(Dirty(editor), "Link edit dirty.");
            var table = i == 0 ? "NavigationItems" : "SocialLinkItems";
            await using (var db = await factory.CreateDbContextAsync())
                await FixtureSqlAsync(db, "CREATE TRIGGER BlockGlobalLink BEFORE UPDATE ON " + table + " BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string, "Link save failure preserves dirty input.");
            await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockGlobalLink;");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Link successful save clears dirty.");

            var newEditor = Activator.CreateInstance(editor.GetType())!;
            var fresh = i == 0 ? (object)new NavigationEditModel() : new SocialLinkEditModel { Platform = SocialPlatform.Telegram };
            WireEditor(newEditor, fresh, i == 0 ? nav : social, "ContentService");
            if (fresh is NavigationEditModel n) n.Label = "New menu"; else ((SocialLinkEditModel)fresh).Url = "https://t.me/example";
            Check(Dirty(newEditor), "New link form tracks edits.");
        }
        await nav.UpdateAsync(1, new() { Label = "Home", Url = "/" });
        await social.UpdateAsync(2, new() { Platform = SocialPlatform.LinkedIn, Url = "" });
        await social.UpdateAsync(2, new() { Platform = SocialPlatform.LinkedIn, Url = null! });
        Check((await social.GetForEditAsync(2)).Url == "", "Cleared optional URL normalizes to decorative empty string.");
    }

    private static async Task CollectionsAsync(NavigationContentService nav, SocialLinkContentService social,
        IDbContextFactory<ApplicationDbContext> factory, HttpClient client, AuthFactory app)
    {
        var newId = await nav.CreateAsync(new() { Label = "Featured project", Url = "/projects/nexconnect" });
        Check((await nav.ListAsync())[^1].Id == newId, "Navigation create appends.");
        await nav.UpdateAsync(newId, new() { Label = "Project spotlight", Url = "/projects/nexconnect" });
        var ids = (await nav.ListAsync()).Select(x => x.Id).Reverse().ToArray();
        await nav.ReorderAsync(ids);
        Check((await nav.ListAsync()).Select(x => x.Id).SequenceEqual(ids) && (await nav.ListAsync()).Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 7)), "Navigation reorder normalized and persistent.");
        await RejectAsync<ValidationException>(() => nav.ReorderAsync([newId, newId]), "Stale/duplicate order rejected.");
        var html = await client.GetStringAsync("/projects/nexconnect");
        Check(html.Contains("Project spotlight") && html.Contains("href=\"/projects/nexconnect\" class=\"active\""), "New navigation item and active detail link rendered.");
        await social.UpdateAsync(2, new() { Platform = SocialPlatform.LinkedIn, Url = "https://www.linkedin.com/company/example" });
        await social.UpdateAsync(3, new() { Platform = SocialPlatform.Telegram, Url = "https://t.me/example" });
        await social.ReorderAsync([3, 1, 2]);
        Check((await social.ListAsync()).Select(x => x.Platform).SequenceEqual(new[] { SocialPlatform.Telegram, SocialPlatform.Email, SocialPlatform.LinkedIn }), "Social update/reorder persisted.");
        await RejectAsync<ValidationException>(() => social.CreateAsync(new() { Platform = SocialPlatform.LinkedIn }), "Duplicate platform rejected.");
        await RejectAsync<ValidationException>(() => social.ReorderAsync([1, 1, 2]), "Invalid social order rejected.");
        await RestartAsync(app);
        Check((await nav.ListAsync()).Select(x => x.Id).SequenceEqual(ids) && (await social.ListAsync())[0].Id == 3, "Collection edits/order survive restart.");
        await nav.DeleteAsync(2);
        await social.DeleteAsync(2);
        await RestartAsync(app);
        Check(!(await nav.ListAsync()).Any(x => x.Id == 2) && !(await social.ListAsync()).Any(x => x.Id == 2), "Delete-one survives restart.");
        foreach (var row in await nav.ListAsync()) await nav.DeleteAsync(row.Id);
        foreach (var row in await social.ListAsync()) await social.DeleteAsync(row.Id);
        await RestartAsync(app);
        Check((await nav.GetAsync()).Count == 0 && (await social.GetAsync()).Count == 0, "Delete-all remains intentionally empty.");
        foreach (var route in new[] { "/", "/about", "/services", "/projects", "/projects/nexconnect", "/team", "/contact" })
        {
            html = await client.GetStringAsync(route);
            var header = html[html.IndexOf("<header", StringComparison.Ordinal)..html.IndexOf("</header>", StringComparison.Ordinal)];
            var footer = html[html.IndexOf("<footer", StringComparison.Ordinal)..];
            Check(header.Contains("NexNovaCo home") && !header.Contains("<li") && !footer.Contains("social_links_wrapp") && !footer.Contains("<li>"),
                "Empty menu/social safe on " + route);
        }
        var customId = await nav.CreateAsync(new() { Label = "Only contact", Url = "/contact" });
        var customSocialId = await social.CreateAsync(new() { Platform = SocialPlatform.Telegram, Url = "https://t.me/example" });
        await RestartAsync(app);
        Check((await nav.ListAsync()).Single().Id == customId && (await social.ListAsync()).Single().Id == customSocialId, "Add-after-empty stays only new content.");
        // Explicit recovery/adoption test: absent markers must not overwrite existing non-empty collections.
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.NavigationInitializationStates.ExecuteDeleteAsync(); await db.SocialLinkInitializationStates.ExecuteDeleteAsync();
            await GlobalSiteInitializer.InitializeAsync(db);
        }
        Check((await nav.ListAsync()).Single().Id == customId && (await social.ListAsync()).Single().Id == customSocialId, "Unmarked pre-existing rows adopted without duplication.");
    }

    private static async Task ImageAsync(GlobalSettingsService service, IMediaStorageService media,
        IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, "image/logo.png"));
        var model = await service.GetSiteIdentityForEditAsync();
        var editor = new GlobalSiteIdentityEditor(); WireEditor(editor, model, service, "GlobalSettings");
        using var field = new CmsImageField();
        SetProperty(field, "Media", media); SetProperty(field, "Value", model.ImagePath); SetProperty(field, "Kind", MediaKind.SiteLogo);
        var selected = await media.ReadAsync(new TestFile(bytes), MediaKind.SiteLogo);
        SetField(field, "_pending", selected); SetField(field, "_selectedName", selected.FileName); SetField(editor, "_imageField", field);
        Check(Dirty(editor), "Logo-only selection tracked by existing guard.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER BlockSiteImage BEFORE UPDATE ON SiteIdentitySettings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
        await CallAsync(editor, "SaveAsync");
        Check(Dirty(editor) && field.HasPendingSelection, "Failed logo save keeps pending image.");
        var path = (string)GetField(field, "_storedPath")!;
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockSiteImage;");
        await CallAsync(editor, "SaveAsync");
        Check(!Dirty(editor) && !field.HasPendingSelection && MediaPolicy.IsGenerated(path, MediaKind.SiteLogo), "Logo saved under uploads/site and dirty cleared.");
        var image = await client.GetAsync("/" + path);
        Check(image.IsSuccessStatusCode && image.Content.Headers.ContentType!.MediaType == "image/png", "Validated uploaded PNG served.");
        var html = await client.GetStringAsync("/");
        Check(System.Text.RegularExpressions.Regex.Matches(html, "<img[^>]*src=\"" + System.Text.RegularExpressions.Regex.Escape(path) + "\"").Count == 2, "Same uploaded logo in Header and Footer.");
        Check(File.Exists(Path.Combine(root, "image/logo.png")), "Original logo untouched.");
        model.ImagePath = "uploads/site/" + new string('a', 32) + ".png";
        await RejectAsync<ValidationException>(() => service.SaveSiteIdentityAsync(model), "Nonexistent media rejected.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            var row = await db.SiteIdentitySettings.SingleAsync(); row.ImagePath = model.ImagePath; await db.SaveChangesAsync();
            Check((await service.ReadSiteIdentityAsync()).ImagePath == "image/logo.png", "Missing logo file renders original fallback.");
            Check((await db.SiteIdentitySettings.AsNoTracking().SingleAsync()).ImagePath == model.ImagePath, "Missing media fallback does not write.");
        }
        await service.SaveSiteIdentityAsync(SiteIdentityEditModel.Approved());
    }

    private static async Task FallbackAsync(GlobalSettingsService service, NavigationContentService nav, SocialLinkContentService social,
        IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        foreach (var table in new[] { "SiteIdentitySettings", "FooterSettings", "NavigationItems", "SocialLinkItems" })
        {
            await using var db = await factory.CreateDbContextAsync();
            var before = await SnapshotGlobalAsync(db);
            await FixtureSqlAsync(db, "ALTER TABLE " + table + " RENAME TO IsolatedUnavailable;");
            try
            {
                switch (table)
                {
                    case "SiteIdentitySettings": Check(JsonSerializer.Serialize(await service.ReadSiteIdentityAsync()) == JsonSerializer.Serialize(SiteIdentityEditModel.Approved()), "Brand read failure falls back."); break;
                    case "FooterSettings": Check(JsonSerializer.Serialize(await service.ReadFooterAsync()) == JsonSerializer.Serialize(FooterEditModel.Approved()), "Footer read failure falls back."); break;
                    case "NavigationItems": Check((await nav.GetAsync()).SequenceEqual(GlobalSiteDefaults.Navigation), "Nav failure uses static defaults."); break;
                    case "SocialLinkItems": Check((await social.GetAsync()).SequenceEqual(GlobalSiteDefaults.SocialLinks), "Social failure uses static defaults."); break;
                }
                Check((await client.GetAsync("/")).IsSuccessStatusCode && (await client.GetAsync("/contact")).IsSuccessStatusCode, "Public shell survives missing " + table);
            }
            finally { await FixtureSqlAsync(db, "ALTER TABLE IsolatedUnavailable RENAME TO " + table + ";"); }
            Check(await SnapshotGlobalAsync(db) == before, "Fallback makes no data/timestamp changes.");
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE NavigationItems SET Url = 'javascript:alert(1)';");
            await db.Database.ExecuteSqlRawAsync("UPDATE SocialLinkItems SET Url = 'data:text/html,bad';");
            var before = await SnapshotGlobalAsync(db);
            Check((await nav.GetAsync()).SequenceEqual(GlobalSiteDefaults.Navigation) && (await social.GetAsync()).SequenceEqual(GlobalSiteDefaults.SocialLinks), "Corrupt stored URLs never rendered.");
            Check(await SnapshotGlobalAsync(db) == before, "Invalid-data fallback makes no writes.");
        }
    }

    private static async Task AuthorizationAsync(TestAuth auth, ClaimsPrincipal principal, ClaimsPrincipal viewer,
        UserManager<ApplicationUser> users, ApplicationUser admin, GlobalSettingsService service,
        NavigationContentService nav, SocialLinkContentService social, ContactPageCmsService contact, IMediaStorageService media)
    {
        Func<Task>[] operations = [
            async () => { await service.GetSiteIdentityForEditAsync(); }, () => service.SaveSiteIdentityAsync(SiteIdentityEditModel.Approved()),
            async () => { await service.GetFooterForEditAsync(); }, () => service.SaveFooterAsync(FooterEditModel.Approved()),
            async () => { await contact.GetSiteContactForEditAsync(); }, () => contact.SaveSiteContactAsync(new()),
            async () => { await nav.ListAsync(); }, async () => { await nav.GetForEditAsync(1); },
            async () => { await nav.CreateAsync(new()); }, () => nav.UpdateAsync(1, new()), () => nav.DeleteAsync(1), () => nav.ReorderAsync([]),
            async () => { await social.ListAsync(); }, async () => { await social.GetForEditAsync(1); },
            async () => { await social.CreateAsync(new()); }, () => social.UpdateAsync(1, new()), () => social.DeleteAsync(1), () => social.ReorderAsync([]),
            async () => { await media.ReadAsync(new TestFile([]), MediaKind.SiteLogo); }
        ];
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), viewer })
        {
            auth.User = identity;
            foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Anonymous/viewer service denied.");
        }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin");
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Revoked Admin denied.");
        await users.AddToRoleAsync(admin, "Admin");
        await users.UpdateSecurityStampAsync(admin);
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Stale stamp denied.");
    }

    private static void Validation()
    {
        foreach (var route in new[] { "/", "about", "/services", "/projects/nexconnect", "/team/emilyjohnson", "/contact" })
            Check(new PublicNavigationRouteAttribute().IsValid(route), "Approved internal route.");
        foreach (var route in new[] { "", " ", "javascript:alert(1)", "data:text/html,x", "//evil.test", "https://example.com", "/dashboard", "/admin/login", "/projects/../admin", "/%2e%2e/admin", "/contact?next=/admin", "about\\evil", "/contact\n" })
            Check(!new PublicNavigationRouteAttribute().IsValid(route), "Unsafe/unsupported navigation rejected.");
        foreach (var url in new[] { "http://example.com", "//example.com", "javascript:alert(1)", "data:text/html,x", "https://u:p@example.com", "https://example.com:8443", "https://example.com\n", "https://example.com\\evil", "/contact", "not a url" })
            Check(!new GlobalSocialUrlAttribute().IsValid(url), "Unsafe social URL rejected.");
        foreach (var url in new[] { "", "https://www.linkedin.com/company/example", "https://t.me/example" })
            Check(new GlobalSocialUrlAttribute().IsValid(url), "Safe social URL/decorative empty allowed.");
        object[] models = [SiteIdentityEditModel.Approved(), FooterEditModel.Approved(), new NavigationEditModel { Label = "Home" },
            new SocialLinkEditModel(), new SiteContactEditModel { Phone = "123", Email = "info@example.invalid", Address = "Address" }];
        foreach (var model in models)
        foreach (var property in model.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            var before = property.GetValue(model);
            if (property.GetCustomAttribute<RequiredAttribute>() is not null)
            {
                property.SetValue(model, " ");
                Check(!Valid(model), "Required " + property.Name);
            }
            property.SetValue(model, new string('x', property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength + 1));
            Check(!Valid(model), "Length " + property.Name);
            property.SetValue(model, before);
        }
        Check(!Valid(new SocialLinkEditModel { Platform = (SocialPlatform)999 }), "Unknown platform rejected.");
        Check(!Valid(new SocialLinkEditModel { Platform = SocialPlatform.Email, Url = "https://example.com" }), "Email cannot duplicate canonical address.");
        Check(!Valid(new SiteContactEditModel { Phone = "123", Email = "bad", Address = "Address" }), "Contact email format checked.");
        Check(!MediaPolicy.IsAllowed("uploads/contact/" + new string('a', 32) + ".png", MediaKind.SiteLogo) &&
            !MediaPolicy.IsAllowed("../logo.png", MediaKind.SiteLogo), "Logo media scope/traversal enforced.");
    }
    private static bool Valid(object model) => Validator.TryValidateObject(model, new ValidationContext(model), [], true);
    private static Task FixtureSqlAsync(ApplicationDbContext db, string sql) => db.Database.ExecuteSqlRawAsync(sql);
    private static async Task<string> SnapshotGlobalAsync(ApplicationDbContext db) => JsonSerializer.Serialize(new {
        Site = await db.SiteIdentitySettings.AsNoTracking().ToArrayAsync(), Footer = await db.FooterSettings.AsNoTracking().ToArrayAsync(),
        Navigation = await db.NavigationItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Social = await db.SocialLinkItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
        NavState = await db.NavigationInitializationStates.AsNoTracking().ToArrayAsync(), SocialState = await db.SocialLinkInitializationStates.AsNoTracking().ToArrayAsync() });
    private static async Task RestartAsync(AuthFactory app)
    {
        await using var restart = new AuthFactory(app.Password, databasePath: app.DatabasePath);
        using var client = restart.NewClient();
        Check((await client.GetAsync("/")).IsSuccessStatusCode, "Separate host restart on isolated database.");
    }
    private static async Task MigrationAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Check(migrations.Length == 22, "One additive global settings migration.");
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        var before = await SnapshotPriorTablesAsync(db, false);
        await db.Database.MigrateAsync();
        await GlobalSiteInitializer.InitializeAsync(db);
        Check(await SnapshotPriorTablesAsync(db, false) == before, "Upgrade preserves every prior row/timestamp.");
        Check(!db.Database.HasPendingModelChanges(), "Migration matches runtime model.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[^1]], db.Database.ProviderName!);
        Check(migration.UpOperations.Count(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation) == 6 &&
            migration.UpOperations.All(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation or Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation), "Exactly six additive tables plus indexes; no changes to existing tables.");
    }
    private static async Task<string> SnapshotPriorTablesAsync(ApplicationDbContext db, bool excludeContact = true)
    {
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT IN ('SiteIdentitySettings', 'FooterSettings', 'NavigationItems', 'NavigationInitializationState', 'SocialLinkItems', 'SocialLinkInitializationState') AND name NOT LIKE '__EF%' AND name <> 'sqlite_sequence' ORDER BY name").ToArrayAsync();
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();
        var snapshot = new List<string>();
        foreach (var name in names)
        {
            if (excludeContact && name == "SiteContactSettings") continue;
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + name.Replace("\"", "\"\"") + "\" ORDER BY rowid";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync()) { var values = new object[reader.FieldCount]; reader.GetValues(values); rows.Add(JsonSerializer.Serialize(values)); }
            snapshot.Add(name + ":" + string.Join("|", rows));
        }
        return string.Join("\n", snapshot);
    }
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static void WireEditor(object editor, object model, object service, string injection)
    {
        SetField(editor, "_model", model); SetProperty(editor, injection, service);
        SetProperty(editor, "Logger", Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(editor.GetType())));
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
    private sealed class TestLogger<T> : ILogger<T>
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
        public string Name => "site-test.png";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => "image/png";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes, false);
    }
}
