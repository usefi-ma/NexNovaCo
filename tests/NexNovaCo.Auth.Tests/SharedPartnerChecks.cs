using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Components.Pages.Dashboard;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class SharedPartnerChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        using var admin = app.NewClient();
        await Login(admin, AuthFactory.Email, app.Password);
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        var viewerPassword = NewPassword();
        ClaimsPrincipal principal;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>()
                .CreateUserPrincipalAsync((await users.FindByEmailAsync(AuthFactory.Email))!);
            Check((await users.CreateAsync(new ApplicationUser { UserName = "shared-viewer@example.invalid", Email = "shared-viewer@example.invalid" }, viewerPassword)).Succeeded, "Shared collection viewer setup.");
        }
        using var viewer = app.NewClient();
        await Login(viewer, "shared-viewer@example.invalid", viewerPassword);
        foreach (var route in new[] { "/dashboard/content/partners", "/dashboard/content/partners/new", "/dashboard/content/partners/1" })
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous shared management route challenges.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin shared management route denied.");
            foreach (var attempt in Enumerable.Range(0, 2))
                Check((await admin.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Admin route supports bookmarks/refresh.");
        }
        Check((await admin.GetStringAsync("/dashboard/content/partners/2147483647")).Contains("no longer exists"), "Missing edit route gives safe feedback.");
        Check((await admin.GetStringAsync("/dashboard/content/partners")).Contains("Shared Content") &&
            (await admin.GetStringAsync("/dashboard/content/home/partners")).Contains("Partners Section"), "Shared collection and Home presentation navigation are distinct.");

        var auth = new TestAuth(principal);
        var service = new PartnerContentService(factory, auth, options, NullLogger<PartnerContentService>.Instance);
        var baseline = await service.GetAsync();
        Check(baseline.Count == 6 && baseline.Select(x => x.Name).SequenceEqual(new[] { "Tech Co", "Digital Co", "NeTech Co", "NeDigital Co", "Alpha Co", "NeAlpha Co" }) && baseline.All(x => PartnerLogoAssets.IsAllowed(x.ImagePath)) && baseline.Count(x => x.HasLogoBackground) == 2 && baseline.All(x => x.Href is null), "Seed preserves approved partner count/order/fields.");
        var initialRows = await service.ListAsync();
        Check(initialRows.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Initial explicit ordering.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            await PartnerInitializer.InitializeAsync(db);
            await PartnerInitializer.InitializeAsync(db);
            Check(await db.Partners.CountAsync() == 6 && !db.Database.HasPendingModelChanges(), "Idempotent initializer and migration model agreement.");
        }

        await using var publicScope = app.Services.CreateAsyncScope();
        var home = publicScope.ServiceProvider.GetRequiredService<IHomeContentService>();
        var about = publicScope.ServiceProvider.GetRequiredService<IAboutContentService>();
        var homeBefore = await home.GetAsync();
        var aboutBefore = await about.GetAsync();
        async Task CheckSharedAsync(string message)
        {
            var expected = await service.GetAsync();
            Check(JsonSerializer.Serialize((await home.GetAsync()).Partners) == JsonSerializer.Serialize(expected) &&
                JsonSerializer.Serialize((await about.GetAsync()).Partners) == JsonSerializer.Serialize(expected), message);
        }
        var model = new PartnerEditModel { Name = "QA Partner", Description = "QA partner description.", ImagePath = PartnerLogoAssets.Paths[0], Href = "https://example.com/partner", HasLogoBackground = true };
        var id = await service.CreateAsync(model);
        Check(id > 0 && !initialRows.Any(x => x.Id == id), "Create uses a new generated identity.");
        var afterCreate = await service.ListAsync();
        Check(afterCreate.Count == 7 && afterCreate[^1].Id == id && afterCreate[^1].DisplayOrder == 7, "Create appends and persists.");
        await CheckSharedAsync("Existing scoped public providers read newly created DB content without stale snapshots.");
        foreach (var route in new[] { "/", "/about" })
            Check((await anonymous.GetStringAsync(route)).Contains("QA Partner"), "Created partner renders on both anonymous pages.");

        model.Name = "QA Edited Partner";
        model.Description = "Edited shared description.\n\nAnother paragraph.";
        await service.UpdateAsync(id, model);
        Check((await service.ListAsync())[^1] is { DisplayOrder: 7, Name: "QA Edited Partner" }, "Edit persists without changing order/identity.");
        var order = afterCreate.Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(order);
        Check((await service.ListAsync()).Select(x => x.Id).SequenceEqual(order), "Explicit reorder persists.");
        await CheckSharedAsync("Home and About consume identical reordered/edited SQLite content.");
        foreach (var route in new[] { "/", "/about" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(html.IndexOf("QA Edited Partner", StringComparison.Ordinal) < html.IndexOf("NeAlpha Co", StringComparison.Ordinal), "Public rendered order reflects database.");
        }
        await using (var db = await factory.CreateDbContextAsync()) await PartnerInitializer.InitializeAsync(db);
        Check((await service.ListAsync())[0].Name == model.Name, "Startup initializer preserves Admin edits and order.");
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var client = restarted.NewClient();
            var html = await client.GetStringAsync("/about");
            Check(html.Contains("Edited shared description.") && html.IndexOf("QA Edited Partner", StringComparison.Ordinal) < html.IndexOf("NeAlpha Co", StringComparison.Ordinal), "Restart preserves added/edited/reordered content.");
            Check((await Login(client, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Shared migration/startup preserves existing Identity password.");
        }

        var orderBefore = JsonSerializer.Serialize(await service.ListAsync());
        foreach (var invalidIds in new[] { new[] { id, id, id }, new[] { id }, new[] { id, -1, int.MaxValue } })
            await ExpectAsync<ValidationException>(() => service.ReorderAsync(invalidIds), "Malformed/stale reorder rejected atomically.");
        Check(JsonSerializer.Serialize(await service.ListAsync()) == orderBefore, "Rejected reorder leaves all data/order untouched.");
        foreach (var property in typeof(PartnerEditModel).GetProperties().Where(x => x.GetCustomAttribute<RequiredAttribute>() is not null))
        {
            var invalid = PartnerEditModel.FromContent(baseline[0]);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Required field rejects whitespace on create.");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Required field rejects whitespace on update.");
            property.SetValue(invalid, new string('x', property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Field max length enforced in service.");
        }
        foreach (var href in new[] { "javascript:alert(1)", "data:text/html,x", "http://example.com", "//example.com", "../about", "/about/../admin", "https://example.com/../secret", "https://example.com/%2e%2e/secret", "https://user:pass@example.com", "https://example.com/evil\\path", "/admin/login", "https://example.com/%252e%252e", " https://example.com", "https://example.com/\npath" })
        {
            var invalid = PartnerEditModel.FromContent(baseline[0]); invalid.Href = href;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Unsafe link rejected by write boundary.");
        }
        foreach (var href in new string?[] { null, "", "/", "services", "/projects/nexconnect", "/team/emilyjohnson", "https://example.com/path?ref=cms#partner" })
        {
            var valid = PartnerEditModel.FromContent(baseline[0]); valid.Href = href;
            Validator.ValidateObject(valid, new ValidationContext(valid), true);
            Check(true, "Safe public routes/HTTPS links accepted.");
        }
        foreach (var path in new[] { "../logo.png", "/image/partnership/TechCo.png", "https://example.com/logo.png", "C:\\logo.png", "image/partnership/missing.png", "image/partnership/%2e%2e/logo.png" })
        {
            var invalid = PartnerEditModel.FromContent(baseline[0]); invalid.ImagePath = path;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Logo allow-list rejects arbitrary paths.");
        }
        model.Description = "<script>alert(1)</script>";
        await service.UpdateAsync(id, model);
        foreach (var route in new[] { "/", "/about" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(html.Contains("&lt;script&gt;") && !html.Contains(model.Description), "Public partner text remain encoded plain text.");
        }
        foreach (var unauthorized in new[] { new ClaimsPrincipal(new ClaimsIdentity()), new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "viewer")], "test")) })
        {
            auth.User = unauthorized;
            await ExpectAsync<UnauthorizedAccessException>(() => service.ListAsync(), "Admin list protected at service boundary.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.GetForEditAsync(id), "Admin editor protected at service boundary.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.CreateAsync(model), "Unauthorized create rejected.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(id, model), "Unauthorized update rejected.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.DeleteAsync(id), "Unauthorized delete rejected.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.ReorderAsync(order), "Unauthorized reorder rejected.");
            Check((await service.GetAsync()).Count == 7, "Public read remains anonymous.");
        }
        auth.User = principal;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(AuthFactory.Email))!;
            await users.RemoveFromRoleAsync(user, "Admin");
            await ExpectAsync<UnauthorizedAccessException>(() => service.DeleteAsync(id), "Revoked role blocks existing circuit writes.");
            await users.AddToRoleAsync(user, "Admin");
            await users.UpdateSecurityStampAsync(user);
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(id, model), "Revoked stamp blocks existing circuit writes.");
            auth.User = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(user);
        }
        await service.DeleteAsync(id);
        await ExpectAsync<KeyNotFoundException>(() => service.DeleteAsync(id), "Already deleted item has safe missing result.");
        await ExpectAsync<KeyNotFoundException>(() => service.UpdateAsync(id, model), "Stale editor cannot recreate deleted item.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(id), "Missing edit read rejected.");
        Check((await service.ListAsync()).Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Delete normalizes remaining order.");
        await CheckSharedAsync("Both public pages reflect deletion.");
        foreach (var route in new[] { "/", "/about" }) Check(!(await anonymous.GetStringAsync(route)).Contains("QA Edited Partner"), "Deleted partner absent publicly.");
        Check((await home.GetAsync()).PartnersHeading == homeBefore.PartnersHeading && (await about.GetAsync()).PartnersHeading == aboutBefore.PartnersHeading, "Shared CRUD leaves page-specific introductions untouched.");
        var homeAfter = await home.GetAsync();
        Check(JsonSerializer.Serialize(homeAfter with { Partners = homeBefore.Partners }) == JsonSerializer.Serialize(homeBefore), "All eight Home settings and unrelated catalogs survive shared CRUD.");

        var unavailable = new PartnerContentService(new UnavailableFactory(), auth, options, NullLogger<PartnerContentService>.Instance);
        Check(JsonSerializer.Serialize(await unavailable.GetAsync()) == JsonSerializer.Serialize(baseline), "Unavailable DB returns exact approved fallback with no writes.");
        await ExpectAsync<SqliteException>(() => unavailable.ListAsync(), "Admin list must not hide database failures using defaults.");
        await ExpectAsync<SqliteException>(() => unavailable.CreateAsync(model), "Write failure propagates for safe editor feedback.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Partners.Where(x => x.Id == initialRows[0].Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Description, ""));
        Check(JsonSerializer.Serialize(await service.GetAsync()) == JsonSerializer.Serialize(baseline), "Invalid stored content safely falls back.");
        // Destructive scenarios are confined to this factory's disposable isolated DB.
        foreach (var row in await service.ListAsync()) await service.DeleteAsync(row.Id);
        Check((await service.GetAsync()).Count == 0, "Empty collection is respected, not replaced by read fallback.");
        foreach (var route in new[] { "/", "/about" })
            Check(!(await anonymous.GetStringAsync(route)).Contains("data-carousel-kind=\"partners\""), "Zero-item state does not mount an empty Owl carousel.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.Partners.CountAsync() == 0, "Empty public read performs no hidden seeding.");
            await PartnerInitializer.InitializeAsync(db);
        }
        Check((await service.GetAsync()).Count == 0, "Controlled startup preserves an intentionally empty initialized collection.");
        await CheckEditorAsync();
    }

    private static async Task CheckEditorAsync()
    {
        var editor = new PartnerEditor();
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var stub = new EditorStub();
        type.GetProperty("PartnersService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<PartnerEditor>.Instance);
        async Task Invoke(string name) => await (Task)type.GetMethod(name, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        foreach (int? id in new int?[] { null, 1 })
        {
            type.GetProperty("Id", flags)!.SetValue(editor, id);
            await Invoke("OnParametersSetAsync");
            Check(!Dirty(), "New/edit form starts clean.");
            var model = (PartnerEditModel)type.GetField("_model", flags)!.GetValue(editor)!;
            foreach (var field in typeof(PartnerEditModel).GetProperties())
            {
                var original = field.GetValue(model);
                field.SetValue(model, field.PropertyType == typeof(bool) ? !(bool)original! : "Changed"); Check(Dirty(), "Every shared field participates in dirty tracking.");
                field.SetValue(model, original); Check(!Dirty(), "Revert clears shared editor dirty state.");
            }
            model.Name = "Test Partner"; model.Description = "Test description."; model.ImagePath = PartnerLogoAssets.Paths[0];
            type.GetMethod("ClearSaved", flags)!.Invoke(editor, null);
            Check(Dirty(), "Invalid submit retains dirty state.");
            foreach (var failure in new Exception[] { new DbUpdateException("Technical detail"), new KeyNotFoundException("Technical detail"), new ValidationException("Technical detail") })
            {
                stub.Failure = failure;
                await Invoke("SaveAsync");
                Check(Dirty() && model.Description == "Test description.", "Failed create/edit retains unsaved content.");
                Check(!((string)type.GetField("_error", flags)!.GetValue(editor)!).Contains("Technical"), "Safe editor error does not expose exception detail.");
            }
            stub.Failure = null;
            await Invoke("SaveAsync");
            Check(!Dirty() && (bool)type.GetField("_saved", flags)!.GetValue(editor)!, "Successful create/edit establishes clean state.");
        }
    }

    private sealed class EditorStub : IPartnerContentService
    {
        public Exception? Failure { get; set; }
        public Task<IReadOnlyList<Partner>> GetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PartnerListItem>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PartnerEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(new PartnerEditModel { Name = "Original Partner", Description = "Original description.", ImagePath = PartnerLogoAssets.Paths[0] });
        public Task<int> CreateAsync(PartnerEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.FromResult(3) : Task.FromException<int>(Failure);
        public Task UpdateAsync(int id, PartnerEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReorderAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private static async Task ExpectAsync<T>(Func<Task> action, string message) where T : Exception
    {
        try { await action(); } catch (T) { Check(true, message); return; }
        Check(false, message);
    }
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public ClaimsPrincipal User { get; set; } = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
    }
    private sealed class UnavailableFactory : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => throw new SqliteException("Test unavailable DB.", 14);
    }
}
