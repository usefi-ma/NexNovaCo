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

internal static class ServicesPageCmsChecks
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
        var service = new ServicesPageCmsService(factory, auth, options, media, NullLogger<ServicesPageCmsService>.Instance);
        var catalog = new ServiceContentService(factory, auth, options, NullLogger<ServiceContentService>.Instance);
        var publicService = new ServicesContentService(catalog, service);
        var sections = new[] { "Hero", "Benefits", "Process", "Pricing", "Faq" };
        var types = new[] { typeof(ServicesHeroEditor), typeof(ServicesBenefitsEditor), typeof(ServicesProcessEditor), typeof(ServicesPricingEditor), typeof(ServicesFaqEditor) };

        var baseline = await service.ReadPublicAsync();
        Check(JsonSerializer.Serialize(baseline) == JsonSerializer.Serialize(ServicesPageDefaults.Content), "Fresh Services content exactly matches approved typed defaults.");
        await RestartAsync(app);
        Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == JsonSerializer.Serialize(baseline), "Restart does not duplicate/default-overwrite Services content.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(!db.Database.HasPendingModelChanges(), "Services migration matches model.");
            Check(await db.ServiceBenefitInitializationStates.CountAsync() == 1 && await db.ServiceProcessInitializationStates.CountAsync() == 1 && await db.ServicePricingInitializationStates.CountAsync() == 1 && await db.ServiceFaqInitializationStates.CountAsync() == 1, "All four collections have persistent seed markers.");
        }
        await MigrationAsync(factory);
        foreach (var row in await service.ListPricingAsync())
        {
            var model = row.ToEditModel();
            Validator.ValidateObject(model, new ValidationContext(model), true);
            Check(true, "Approved currency/grouped price and features validate without relying on public fallback.");
        }
        Check((await anonymous.GetStringAsync("/services")).Contains("http://localhost/image/about/2025timeline.jpg") ||
              (await anonymous.GetStringAsync("/services")).Contains("https://localhost/image/about/2025timeline.jpg"), "CSS media uses base-aware absolute URLs, not stylesheet-relative paths.");

        var viewerPassword = NewPassword();
        var viewerUser = new ApplicationUser { Email = "services-viewer@example.invalid", UserName = "services-viewer@example.invalid" };
        Check((await users.CreateAsync(viewerUser, viewerPassword)).Succeeded, "Create isolated non-Admin viewer.");
        using var viewer = app.NewClient();
        await Login(viewer, viewerUser.Email, viewerPassword);
        var routes = sections.Select(x => "/dashboard/content/services/" + x.ToLowerInvariant())
            .Concat(new[] { "/dashboard/content/services", "/dashboard/content/services/overview",
                "/dashboard/content/services/benefits/new", "/dashboard/content/services/benefits/1", "/dashboard/content/services/process/new", "/dashboard/content/services/process/1", "/dashboard/content/services/pricing/new", "/dashboard/content/services/pricing/1", "/dashboard/content/services/faq/new", "/dashboard/content/services/faq/1" });
        foreach (var route in routes)
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous Services route challenges: " + route);
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin Services route denied: " + route);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var response = await adminClient.GetAsync(route);
                Check(response.IsSuccessStatusCode || route == "/dashboard/content/services" && response.StatusCode == HttpStatusCode.Redirect, "Admin direct/refresh route: " + route);
            }
        }
        Check((await adminClient.GetStringAsync("/dashboard/content/services/benefits/2147483647")).Contains("no longer exists"), "Missing Benefit item safe.");
        Check((await adminClient.GetStringAsync("/dashboard/content/services/process/2147483647")).Contains("no longer exists"), "Missing Process item safe.");
        Check((await adminClient.GetStringAsync("/dashboard/content/services/pricing/2147483647")).Contains("no longer exists"), "Missing Pricing item safe.");
        Check((await adminClient.GetStringAsync("/dashboard/content/services/faq/2147483647")).Contains("no longer exists"), "Missing Faq item safe.");
        Check((await adminClient.GetAsync("/dashboard/content/shared-services")).IsSuccessStatusCode, "Shared Services Admin smoke.");
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
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockServicesSave BEFORE UPDATE ON Services" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && GetField(editor, "_error") is string && !(bool)GetField(editor, "_saved")!, section + " failed save stays dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockServicesSave;");
            title.SetValue(model, "");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, section + " server validation failure stays dirty.");
            title.SetValue(model, section + " isolated edit");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, section + " successful save clean.");
            Check((await anonymous.GetStringAsync("/services")).Contains(section + " isolated edit"), section + " public SQLite edit visible.");
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

        var first = (await catalog.ListAsync()).First(x => x.HomeDisplayOrder is not null);
        var original = await catalog.GetForEditAsync(first.Id);
        var edited = await catalog.GetForEditAsync(first.Id);
        edited.Name = "Shared Services QA Card";
        await catalog.UpdateAsync(first.Id, edited);
        Check((await publicService.GetAsync()).Services.Any(x => x.Name == edited.Name), "Services uses canonical shared catalog.");
        Check((await anonymous.GetStringAsync("/")).Contains(edited.Name) && (await anonymous.GetStringAsync("/services")).Contains(edited.Name), "Shared edit visible on Home and Services.");
        await catalog.UpdateAsync(first.Id, original);

        var operations = new Func<Task>[]
        {
            () => service.GetHeroForEditAsync(),
            () => service.SaveHeroAsync(ServicesHeroEditModel.Approved()),
            () => service.GetBenefitsForEditAsync(),
            () => service.SaveBenefitsAsync(ServicesBenefitsEditModel.Approved()),
            () => service.GetProcessForEditAsync(),
            () => service.SaveProcessAsync(ServicesProcessEditModel.Approved()),
            () => service.GetPricingForEditAsync(),
            () => service.SavePricingAsync(ServicesPricingEditModel.Approved()),
            () => service.GetFaqForEditAsync(),
            () => service.SaveFaqAsync(ServicesFaqEditModel.Approved()),
            () => service.ListBenefitAsync(),
            () => service.GetBenefitItemForEditAsync(1),
            () => service.CreateBenefitAsync(new()),
            () => service.UpdateBenefitAsync(1, new()),
            () => service.DeleteBenefitAsync(1),
            () => service.ReorderBenefitAsync([]),
            () => service.ListProcessAsync(),
            () => service.GetProcessItemForEditAsync(1),
            () => service.CreateProcessAsync(new()),
            () => service.UpdateProcessAsync(1, new()),
            () => service.DeleteProcessAsync(1),
            () => service.ReorderProcessAsync([]),
            () => service.ListPricingAsync(),
            () => service.GetPricingItemForEditAsync(1),
            () => service.CreatePricingAsync(new()),
            () => service.UpdatePricingAsync(1, new()),
            () => service.DeletePricingAsync(1),
            () => service.ReorderPricingAsync([]),
            () => service.ListFaqAsync(),
            () => service.GetFaqItemForEditAsync(1),
            () => service.CreateFaqAsync(new()),
            () => service.UpdateFaqAsync(1, new()),
            () => service.DeleteFaqAsync(1),
            () => service.ReorderFaqAsync([])
        };
        foreach (var identity in new[] { new ClaimsPrincipal(new ClaimsIdentity()), await signIn.CreateUserPrincipalAsync(viewerUser) })
        {
            auth.User = identity;
            foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Every Admin read/write denies anonymous/non-Admin service invocation.");
            Check((await service.ReadPublicAsync()).Hero.Title == ServicesPageDefaults.Content.Hero.Title, "Public Services read remains anonymous.");
        }
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin, "Admin");
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Live role revocation enforced.");
        await users.AddToRoleAsync(admin, "Admin");
        await users.UpdateSecurityStampAsync(admin);
        foreach (var operation in operations) await RejectAsync<UnauthorizedAccessException>(operation, "Security-stamp revocation enforced.");
        Validation();
    }

    private static async Task CollectionsAsync(AuthFactory app, ServicesPageCmsService service, HttpClient client)
    {
        {
            var approved = await service.ListBenefitAsync();
            Check(approved.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, approved.Count)), "Benefit approved order.");
            var model = approved[0].ToEditModel(); model.Title = "Added Benefit";
            var id = await service.CreateBenefitAsync(model);
            model.Title = "Updated Benefit";
            await service.UpdateBenefitAsync(id, model);
            var ids = (await service.ListBenefitAsync()).Select(x => x.Id).Reverse().ToArray();
            await service.ReorderBenefitAsync(ids);
            await RestartAsync(app);
            Check((await service.ListBenefitAsync()).Select(x => x.Id).SequenceEqual(ids), "Benefit create/edit/reorder survive restart.");
            Check((await client.GetStringAsync("/services")).Contains("Updated Benefit"), "Public Benefit update visible, extra items safe.");
            Check(JsonSerializer.Serialize(await service.GetBenefitItemForEditAsync(id)) == JsonSerializer.Serialize(model), "Benefit fields/child order persist.");
            await RejectAsync<ValidationException>(() => service.ReorderBenefitAsync([id, id]), "Benefit stale/duplicate order rejected.");
            await service.DeleteBenefitAsync(id); await RestartAsync(app);
            Check((await service.ListBenefitAsync()).All(x => x.Id != id), "Benefit delete one persists.");
            foreach (var row in await service.ListBenefitAsync()) await service.DeleteBenefitAsync(row.Id);
            await RestartAsync(app);
            Check((await service.ListBenefitAsync()).Count == 0 && (await service.ReadPublicAsync()).Benefits.Count == 0, "Benefit delete all remains empty without fallback.");
            var html = await client.GetStringAsync("/services");
            Check(!html.Contains("features_inner_hexagon"), "Benefit empty rendering omits cards/diagram/accordion.");
            id = await service.CreateBenefitAsync(model); await RestartAsync(app);
            Check((await service.ListBenefitAsync()).Single().Id == id, "Benefit add after empty keeps only new item.");
            await service.DeleteBenefitAsync(id);
            foreach (var row in approved) await service.CreateBenefitAsync(row.ToEditModel());
        }
        {
            var approved = await service.ListProcessAsync();
            Check(approved.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, approved.Count)), "Process approved order.");
            var model = approved[0].ToEditModel(); model.Title = "Added Process";
            var id = await service.CreateProcessAsync(model);
            model.Title = "Updated Process";
            await service.UpdateProcessAsync(id, model);
            var ids = (await service.ListProcessAsync()).Select(x => x.Id).Reverse().ToArray();
            await service.ReorderProcessAsync(ids);
            await RestartAsync(app);
            Check((await service.ListProcessAsync()).Select(x => x.Id).SequenceEqual(ids), "Process create/edit/reorder survive restart.");
            Check((await client.GetStringAsync("/services")).Contains("Updated Process"), "Public Process update visible, extra items safe.");
            Check(JsonSerializer.Serialize(await service.GetProcessItemForEditAsync(id)) == JsonSerializer.Serialize(model), "Process fields/child order persist.");
            await RejectAsync<ValidationException>(() => service.ReorderProcessAsync([id, id]), "Process stale/duplicate order rejected.");
            await service.DeleteProcessAsync(id); await RestartAsync(app);
            Check((await service.ListProcessAsync()).All(x => x.Id != id), "Process delete one persists.");
            foreach (var row in await service.ListProcessAsync()) await service.DeleteProcessAsync(row.Id);
            await RestartAsync(app);
            Check((await service.ListProcessAsync()).Count == 0 && (await service.ReadPublicAsync()).Steps.Count == 0, "Process delete all remains empty without fallback.");
            var html = await client.GetStringAsync("/services");
            Check(!html.Contains("main_hexagon_wrapper"), "Process empty rendering omits cards/diagram/accordion.");
            id = await service.CreateProcessAsync(model); await RestartAsync(app);
            Check((await service.ListProcessAsync()).Single().Id == id, "Process add after empty keeps only new item.");
            await service.DeleteProcessAsync(id);
            foreach (var row in approved) await service.CreateProcessAsync(row.ToEditModel());
        }
        {
            var approved = await service.ListPricingAsync();
            Check(approved.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, approved.Count)), "Pricing approved order.");
            var model = approved[0].ToEditModel(); model.Name = "Added Pricing";
            var id = await service.CreatePricingAsync(model);
            model.Name = "Updated Pricing";
            model.Features = ["Second feature", "First feature", "Third feature"];
            await service.UpdatePricingAsync(id, model);
            var ids = (await service.ListPricingAsync()).Select(x => x.Id).Reverse().ToArray();
            await service.ReorderPricingAsync(ids);
            await RestartAsync(app);
            Check((await service.ListPricingAsync()).Select(x => x.Id).SequenceEqual(ids), "Pricing create/edit/reorder survive restart.");
            foreach (var p in await service.ListPricingAsync()) { var m = p.ToEditModel(); Validator.ValidateObject(m, new ValidationContext(m), true); }
            Check((await client.GetStringAsync("/services")).Contains("Updated Pricing"), "Public Pricing update visible, extra items safe.");
            Check(JsonSerializer.Serialize(await service.GetPricingItemForEditAsync(id)) == JsonSerializer.Serialize(model), "Pricing fields/child order persist.");
            await RejectAsync<ValidationException>(() => service.ReorderPricingAsync([id, id]), "Pricing stale/duplicate order rejected.");
            await service.DeletePricingAsync(id); await RestartAsync(app);
            Check((await service.ListPricingAsync()).All(x => x.Id != id), "Pricing delete one persists.");
            foreach (var row in await service.ListPricingAsync()) await service.DeletePricingAsync(row.Id);
            await RestartAsync(app);
            Check((await service.ListPricingAsync()).Count == 0 && (await service.ReadPublicAsync()).Plans.Count == 0, "Pricing delete all remains empty without fallback.");
            var html = await client.GetStringAsync("/services");
            Check(!html.Contains("class=\"price_item"), "Pricing empty rendering omits cards/diagram/accordion.");
            id = await service.CreatePricingAsync(model); await RestartAsync(app);
            Check((await service.ListPricingAsync()).Single().Id == id, "Pricing add after empty keeps only new item.");
            await service.DeletePricingAsync(id);
            foreach (var row in approved) await service.CreatePricingAsync(row.ToEditModel());
        }
        {
            var approved = await service.ListFaqAsync();
            Check(approved.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, approved.Count)), "Faq approved order.");
            var model = approved[0].ToEditModel(); model.Question = "Added Faq";
            var id = await service.CreateFaqAsync(model);
            model.Question = "Updated Faq";
            await service.UpdateFaqAsync(id, model);
            var ids = (await service.ListFaqAsync()).Select(x => x.Id).Reverse().ToArray();
            await service.ReorderFaqAsync(ids);
            await RestartAsync(app);
            Check((await service.ListFaqAsync()).Select(x => x.Id).SequenceEqual(ids), "Faq create/edit/reorder survive restart.");
            Check((await client.GetStringAsync("/services")).Contains("Updated Faq"), "Public Faq update visible, extra items safe.");
            Check(JsonSerializer.Serialize(await service.GetFaqItemForEditAsync(id)) == JsonSerializer.Serialize(model), "Faq fields/child order persist.");
            await RejectAsync<ValidationException>(() => service.ReorderFaqAsync([id, id]), "Faq stale/duplicate order rejected.");
            await service.DeleteFaqAsync(id); await RestartAsync(app);
            Check((await service.ListFaqAsync()).All(x => x.Id != id), "Faq delete one persists.");
            foreach (var row in await service.ListFaqAsync()) await service.DeleteFaqAsync(row.Id);
            await RestartAsync(app);
            Check((await service.ListFaqAsync()).Count == 0 && (await service.ReadPublicAsync()).Questions.Count == 0, "Faq delete all remains empty without fallback.");
            var html = await client.GetStringAsync("/services");
            Check(!html.Contains("accordion-item"), "Faq empty rendering omits cards/diagram/accordion.");
            id = await service.CreateFaqAsync(model); await RestartAsync(app);
            Check((await service.ListFaqAsync()).Single().Id == id, "Faq add after empty keeps only new item.");
            await service.DeleteFaqAsync(id);
            foreach (var row in approved) await service.CreateFaqAsync(row.ToEditModel());
        }
        var plans = await service.ListPricingAsync();
        var featured = plans.Single(x => x.Treatment == PricingTreatment.Premium);
        await RejectAsync<ValidationException>(() => service.CreatePricingAsync(featured.ToEditModel()), "Second highlighted plan rejected.");
        var basic = plans.First(x => x.Treatment == PricingTreatment.Basic);
        var change = basic.ToEditModel(); change.Treatment = PricingTreatment.Premium;
        await RejectAsync<ValidationException>(() => service.UpdatePricingAsync(basic.Id, change), "Update cannot create second highlighted plan.");
        change = featured.ToEditModel(); change.Treatment = PricingTreatment.Standard;
        await service.UpdatePricingAsync(featured.Id, change);
        change = basic.ToEditModel(); change.Treatment = PricingTreatment.Premium;
        await service.UpdatePricingAsync(basic.Id, change); await RestartAsync(app);
        Check((await service.ListPricingAsync()).Single(x => x.Treatment == PricingTreatment.Premium).Id == basic.Id, "Featured treatment can move explicitly.");
        await service.UpdatePricingAsync(basic.Id, basic.ToEditModel());
        await service.UpdatePricingAsync(featured.Id, featured.ToEditModel());
    }

    private static async Task ImagesAsync(ServicesPageCmsService service, IMediaStorageService media, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.ServicesHeroDefault));
        foreach (var kind in new[] { MediaKind.ServicesHero, MediaKind.ServicesBenefits })
        {
            var section = kind == MediaKind.ServicesHero ? "Hero" : "Benefits";
            var model = (await CallAsync(service, "Get" + section + "ForEditAsync", CancellationToken.None))!;
            object editor = kind == MediaKind.ServicesHero ? new ServicesHeroEditor() : new ServicesBenefitsEditor();
            WireEditor(editor, model, service);
            var field = new CmsImageField();
            SetProperty(field, "Media", media); SetProperty(field, "Value", model.GetType().GetProperty("ImagePath")!.GetValue(model));
            SetProperty(field, "Kind", kind);
            var selected = await media.ReadAsync(new TestFile(bytes), kind);
            SetField(field, "_pending", selected); SetField(field, "_selectedName", selected.FileName);
            SetField(editor, "_imageField", field);
            Check(Dirty(editor), section + " image-only selection dirty.");
            await using (var db = await factory.CreateDbContextAsync())
                await ExecuteFixtureDdlAsync(db, "CREATE TRIGGER BlockServicesImage BEFORE UPDATE ON Services" + section + "Settings BEGIN SELECT RAISE(ABORT, 'isolated failure'); END;");
            await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && field.HasPendingSelection, section + " failed DB save keeps selected image/dirty state.");
            var path = (string)GetField(field, "_storedPath")!;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockServicesImage;");
            await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && !field.HasPendingSelection && MediaPolicy.IsGenerated(path, kind), section + " retry reuses validated upload and marks clean.");
            var image = await client.GetAsync("/" + path);
            Check(image.IsSuccessStatusCode && image.Content.Headers.ContentType!.MediaType == "image/jpeg" && image.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "Services media served as safe raster.");
            Check((await client.GetStringAsync("/services")).Contains(path), section + " public image path from database.");
            var approved = model.GetType().GetMethod("Approved")!.Invoke(null, null)!;
            await CallAsync(service, "Save" + section + "Async", approved, CancellationToken.None);
            field.Dispose();
        }
    }

    private static async Task CollectionEditorAsync(ServicesPageCmsService service)
    {
        {
            var row = (await service.ListBenefitAsync())[0];
            var editor = new ServicesBenefitItemEditor(); var model = row.ToEditModel();
            WireEditor(editor, model, service); SetField(editor, "_editingId", (int?)row.Id); SetProperty(editor, "Id", (int?)row.Id);
            Check(!Dirty(editor), "Benefit editor loads clean.");
            var original = model.Title; model.Title = "";
            Check(Dirty(editor), "Benefit dirty."); await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, "Benefit validation failure retains dirty state.");
            model.Title = "Handler edited Benefit"; await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Benefit save clean.");
            model.Title = original; await CallAsync(editor, "SaveAsync");
        }
        {
            var row = (await service.ListProcessAsync())[0];
            var editor = new ServicesProcessItemEditor(); var model = row.ToEditModel();
            WireEditor(editor, model, service); SetField(editor, "_editingId", (int?)row.Id); SetProperty(editor, "Id", (int?)row.Id);
            Check(!Dirty(editor), "Process editor loads clean.");
            var original = model.Title; model.Title = "";
            Check(Dirty(editor), "Process dirty."); await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, "Process validation failure retains dirty state.");
            model.Title = "Handler edited Process"; await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Process save clean.");
            model.Title = original; await CallAsync(editor, "SaveAsync");
        }
        {
            var row = (await service.ListPricingAsync())[0];
            var editor = new ServicesPricingItemEditor(); var model = row.ToEditModel();
            WireEditor(editor, model, service); SetField(editor, "_editingId", (int?)row.Id); SetProperty(editor, "Id", (int?)row.Id);
            Check(!Dirty(editor), "Pricing editor loads clean.");
            var original = model.Name; model.Name = "";
            Check(Dirty(editor), "Pricing dirty."); await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, "Pricing validation failure retains dirty state.");
            model.Name = "Handler edited Pricing"; await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Pricing save clean.");
            model.Name = original; await CallAsync(editor, "SaveAsync");
            var features = model.Features.ToList(); model.Features.Add("New feature");
            Check(Dirty(editor), "Pricing child-only edit dirty."); await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor), "Pricing child-only save clean."); model.Features = features; await CallAsync(editor, "SaveAsync");
        }
        {
            var row = (await service.ListFaqAsync())[0];
            var editor = new ServicesFaqItemEditor(); var model = row.ToEditModel();
            WireEditor(editor, model, service); SetField(editor, "_editingId", (int?)row.Id); SetProperty(editor, "Id", (int?)row.Id);
            Check(!Dirty(editor), "Faq editor loads clean.");
            var original = model.Question; model.Question = "";
            Check(Dirty(editor), "Faq dirty."); await CallAsync(editor, "SaveAsync");
            Check(Dirty(editor) && !(bool)GetField(editor, "_saved")!, "Faq validation failure retains dirty state.");
            model.Question = "Handler edited Faq"; await CallAsync(editor, "SaveAsync");
            Check(!Dirty(editor) && (bool)GetField(editor, "_saved")!, "Faq save clean.");
            model.Question = original; await CallAsync(editor, "SaveAsync");
        }
    }

    private static async Task FallbackAsync(ServicesPageCmsService service, IDbContextFactory<ApplicationDbContext> factory, HttpClient client)
    {
        var before = JsonSerializer.Serialize(await service.ReadPublicAsync());
        foreach (var table in new[] { "ServicesHeroSettings", "ServicesBenefitsSettings", "ServicesProcessSettings", "ServicesPricingSettings", "ServicesFaqSettings", "ServiceBenefits", "ServiceProcessSteps", "ServicePricingPlans", "ServiceFaqItems" })
        {
            await using var db = await factory.CreateDbContextAsync();
            await ExecuteFixtureDdlAsync(db, "ALTER TABLE " + table + " RENAME TO IsolatedUnavailable;");
            try
            {
                Check(JsonSerializer.Serialize(await service.ReadPublicAsync()) == before, table + " read failure uses approved slice defaults.");
                Check((await client.GetAsync("/services")).IsSuccessStatusCode, table + " missing slice does not crash public.");
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
        var servicesIndex = Array.FindIndex(migrations, x => x.EndsWith("_AddCompleteServicesPageCms", StringComparison.Ordinal));
        Check(servicesIndex == 17, "One additive Services migration.");
        // This is exclusively AuthFactory's disposable database, not the normal developer database.
        await db.GetService<IMigrator>().MigrateAsync(migrations[servicesIndex - 1]);
        var before = await SnapshotPriorTablesAsync(db);
        await db.GetService<IMigrator>().MigrateAsync(migrations[servicesIndex]);
        await ServicesPageInitializer.InitializeAsync(db);
        Check(await SnapshotPriorTablesAsync(db) == before, "Upgrade preserves every prior Identity/Home/shared table, row, timestamp and account.");
        await db.Database.MigrateAsync();
        await ProjectsPageInitializer.InitializeAsync(db);
        await TeamPageInitializer.InitializeAsync(db);
        await ContactPageInitializer.InitializeAsync(db);
        await GlobalSiteInitializer.InitializeAsync(db);
        Check(!db.Database.HasPendingModelChanges(), "Upgrade leaves no pending model changes.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[servicesIndex]], db.Database.ProviderName!);
        Check(migration.UpOperations.All(x => x is Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation or Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation), "Services migration only creates its tables/indexes; no destructive operations.");
    }
    private static async Task<string> SnapshotPriorTablesAsync(ApplicationDbContext db)
    {
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'Services%Settings' AND name NOT LIKE 'Projects%Settings' AND name NOT LIKE 'Team%Settings' AND name NOT LIKE 'ServiceBenefit%' AND name NOT LIKE 'ServiceProcess%' AND name NOT LIKE 'ServicePricing%' AND name NOT LIKE 'ServiceFaq%' AND name NOT LIKE '__EF%' AND name <> 'sqlite_sequence' ORDER BY name").ToArrayAsync();
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
        Check((await client.GetAsync("/services")).IsSuccessStatusCode, "Separate host restart on isolated existing database.");
    }
    private static void Validation()
    {
        foreach (var route in new[] { "/", "services", "/projects/nexconnect", "/team/alex", "services#Service", "/services#Service" })
            Check(new ServicesPageCtaRouteAttribute().IsValid(route), "Approved internal CTA accepted.");
        foreach (var route in new[] { "//evil.test", "https://evil.test", "javascript:alert(1)", "/dashboard", "/services?x=1", "/services#fake", "/services\n", "/../admin", "/%2fadmin" })
            Check(!new ServicesPageCtaRouteAttribute().IsValid(route), "Unsafe CTA rejected.");
        foreach (var model in new object[] { new ServiceBenefitEditModel { Title = new string('x',81), Description = "x" }, new ServiceProcessEditModel { Title = "x", Icon = (ProcessIcon)99 }, new ServiceFaqEditModel { Question = "x", Answer = "" }, new ServicePricingEditModel { Name = "x", Subtitle = "x", DisplayPrice = "CAD -5", CtaLabel = "x", IdealFor = "x" } })
            Check(!Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true), "Field limits/enums/price validation enforced.");
    }
    private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static void WireEditor(object editor, object model, IServicesPageCmsService service)
    {
        SetField(editor, "_model", model); SetProperty(editor, "ServicesPageService", service);
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
