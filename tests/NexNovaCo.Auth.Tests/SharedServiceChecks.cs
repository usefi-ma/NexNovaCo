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

internal static class SharedServiceChecks
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
        foreach (var route in new[] { "/dashboard/content/shared-services", "/dashboard/content/shared-services/new", "/dashboard/content/shared-services/1", "/dashboard/content/shared-services/home-featured" })
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous shared management route challenges.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin shared management route denied.");
            foreach (var attempt in Enumerable.Range(0, 2))
                Check((await admin.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Admin route supports bookmarks/refresh.");
        }
        Check((await admin.GetStringAsync("/dashboard/content/shared-services/2147483647")).Contains("no longer exists"), "Missing edit route gives safe feedback.");
        Check((await admin.GetStringAsync("/dashboard/content/shared-services")).Contains("Shared Content") &&
            (await admin.GetStringAsync("/dashboard/content/home/services")).Contains("Services Section"), "Shared collection and Home presentation navigation are distinct.");

        var auth = new TestAuth(principal);
        var service = new ServiceContentService(factory, auth, options, NullLogger<ServiceContentService>.Instance);
        var initial = await service.GetAsync();
        var initialFeatured = await service.GetHomeFeaturedAsync();
        Check(initial.Select(x => x.Id).SequenceEqual(new[] { "software", "web-mobile", "ai", "consulting", "design", "strategy" }), "Exact original canonical Service identities/order retained.");
        Check(initial.Select(x => x.IconPath).SequenceEqual(ServiceIconAssets.Paths), "Approved icons keep canonical assignments.");
        Check(initialFeatured.SequenceEqual(initial.Take(5)), "Original Home subset and independent order seeded exactly.");
        var rows = await service.ListAsync();
        Check(rows.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Shared explicit order.");
        Check(rows.Take(5).Select(x => x.HomeDisplayOrder).SequenceEqual(Enumerable.Range(1, 5).Select(x => (int?)x)) && rows[^1].HomeDisplayOrder is null, "Admin list shows correct Home membership/order.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            await ServiceInitializer.InitializeAsync(db); await ServiceInitializer.InitializeAsync(db);
            Check(await db.Services.CountAsync() == 6 && await db.HomeFeaturedServices.CountAsync() == 5 &&
                await db.ServiceInitializationStates.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Idempotent atomic seed and migration model agree.");
        }
        await using var publicScope = app.Services.CreateAsyncScope();
        var home = publicScope.ServiceProvider.GetRequiredService<IHomeContentService>();
        var page = publicScope.ServiceProvider.GetRequiredService<IServicesContentService>();
        var homeBefore = await home.GetAsync();
        var pageBefore = await page.GetAsync();
        async Task CheckPublicAsync()
        {
            Check(JsonSerializer.Serialize((await page.GetAsync()).Services) == JsonSerializer.Serialize(await service.GetAsync()), "Services page reads fresh full database content.");
            Check(JsonSerializer.Serialize((await home.GetAsync()).Services) == JsonSerializer.Serialize(await service.GetHomeFeaturedAsync()), "Home reads fresh joined featured content.");
        }
        var model = new ServiceEditModel { Name = "QA Service", Tagline = "QA tagline", Description = "QA description.", IconPath = ServiceIconAssets.Paths[0] };
        var id = await service.CreateAsync(model);
        var created = (await service.GetAsync())[^1];
        Check((await service.ListAsync())[^1] is { DisplayOrder: 7, HomeDisplayOrder: null }, "New Service appends without auto-featuring.");
        Check((await anonymous.GetStringAsync("/services")).Contains(model.Name) && !(await anonymous.GetStringAsync("/")).Contains(model.Name), "Create appears only on full catalog before selection.");
        var featuredIds = new[] { id, rows[1].Id, rows[0].Id };
        await service.SaveHomeFeaturedAsync(featuredIds);
        Check((await service.GetHomeFeaturedAsync()).Select(x => x.Name).SequenceEqual(new[] { model.Name, initial[1].Name, initial[0].Name }), "Featured save adds/removes/joins canonical entities in Home order.");
        Check((await anonymous.GetStringAsync("/")).Contains(model.Name), "Selected Service appears on Home.");
        model.Name = "QA Edited Service"; model.Tagline = "Edited tagline";
        await service.UpdateAsync(id, model);
        Check((await service.GetAsync())[^1].Id == created.Id && (await service.GetHomeFeaturedAsync())[0].Name == model.Name, "Edit retains identity and changes both consumers without copies.");
        var featuredBefore = JsonSerializer.Serialize(await service.GetHomeFeaturedAsync());
        var order = (await service.ListAsync()).Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(order);
        Check((await service.ListAsync()).Select(x => x.Id).SequenceEqual(order), "Shared reorder persists.");
        Check(JsonSerializer.Serialize(await service.GetHomeFeaturedAsync()) == featuredBefore, "Shared order never changes Home order.");
        var sharedBefore = JsonSerializer.Serialize(await service.GetAsync());
        await service.SaveHomeFeaturedAsync(featuredIds.Reverse().ToArray());
        Check(JsonSerializer.Serialize(await service.GetAsync()) == sharedBefore, "Featured reorder never changes Services page order.");
        Check((await service.GetHomeFeaturedAsync()).Select(x => x.Name).SequenceEqual(new[] { initial[0].Name, initial[1].Name, model.Name }), "Featured reorder persisted independently.");
        await CheckPublicAsync();

        var listBefore = JsonSerializer.Serialize(await service.ListAsync());
        foreach (var invalid in new[] { new[] { id, id }, new[] { int.MaxValue }, Array.Empty<int>() })
            await ExpectAsync<ValidationException>(() => service.ReorderAsync(invalid), "Incomplete/duplicate/stale shared reorder rejected.");
        foreach (var invalid in new[] { new[] { id, id }, new[] { int.MaxValue }, rows.Select(x => x.Id).ToArray() })
            await ExpectAsync<ValidationException>(() => service.SaveHomeFeaturedAsync(invalid), "Duplicate/missing/over-capacity featured selection rejected.");
        Check(JsonSerializer.Serialize(await service.ListAsync()) == listBefore, "Rejected operations do not partially write either order.");
        foreach (var property in typeof(ServiceEditModel).GetProperties())
        {
            var invalid = ServiceEditModel.FromContent(initial[0]);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Required field enforced on create.");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Required field enforced on update.");
            property.SetValue(invalid, new string('x', property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Service field length enforced.");
        }
        foreach (var icon in new[] { "../secret.png", "C:/secret.png", "/image/service/icons/software.png", "https://example.com/icon.png", "image/service/icons/%2e%2e/x.png", "image/service/icons/missing.png" })
        {
            var invalid = ServiceEditModel.FromContent(initial[0]); invalid.IconPath = icon;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Only approved icon paths accepted.");
        }
        model.Description = "<script>alert(1)</script>";
        await service.UpdateAsync(id, model);
        foreach (var route in new[] { "/", "/services" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(html.Contains("&lt;script&gt;") && !html.Contains(model.Description), "Shared text is HTML encoded.");
        }
        foreach (var unauthorized in new[] { new ClaimsPrincipal(new ClaimsIdentity()), new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "viewer")], "test")) })
        {
            auth.User = unauthorized;
            await ExpectAsync<UnauthorizedAccessException>(() => service.ListAsync(), "Admin list service boundary.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.GetForEditAsync(id), "Admin editor service boundary.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.CreateAsync(model), "Unauthorized create blocked.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(id, model), "Unauthorized update blocked.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.DeleteAsync(id), "Unauthorized delete blocked.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.ReorderAsync(order), "Unauthorized shared reorder blocked.");
            await ExpectAsync<UnauthorizedAccessException>(() => service.SaveHomeFeaturedAsync([]), "Unauthorized featured clear blocked.");
            Check((await service.GetAsync()).Count == 7 && (await service.GetHomeFeaturedAsync()).Count == 3, "Both public reads remain anonymous.");
        }
        auth.User = principal;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(AuthFactory.Email))!;
            await users.RemoveFromRoleAsync(user, "Admin");
            await ExpectAsync<UnauthorizedAccessException>(() => service.DeleteAsync(id), "Revoked role denies stale circuit.");
            await users.AddToRoleAsync(user, "Admin"); await users.UpdateSecurityStampAsync(user);
            await ExpectAsync<UnauthorizedAccessException>(() => service.SaveHomeFeaturedAsync([]), "Revoked stamp denies featured write.");
            auth.User = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(user);
        }
        await service.DeleteAsync(id);
        await ExpectAsync<KeyNotFoundException>(() => service.DeleteAsync(id), "Repeated delete safe.");
        await ExpectAsync<KeyNotFoundException>(() => service.UpdateAsync(id, model), "Deleted Service cannot be recreated by stale edit.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(id), "Missing Service editor safe.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(!await db.HomeFeaturedServices.AnyAsync(x => x.ServiceId == id) && await db.HomeFeaturedServices.CountAsync() == 2, "Delete cascades selection and preserves others.");
        Check((await service.ListAsync()).Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Delete normalizes shared positions.");
        Check((await service.ListAsync()).Where(x => x.HomeDisplayOrder != null).OrderBy(x => x.HomeDisplayOrder).Select(x => x.HomeDisplayOrder).SequenceEqual(new int?[] { 1, 2 }), "Delete normalizes featured positions.");
        await CheckPublicAsync();
        foreach (var route in new[] { "/", "/services" }) Check(!(await anonymous.GetStringAsync(route)).Contains(model.Name), "Delete removed both public occurrences.");
        Check(JsonSerializer.Serialize((await home.GetAsync()) with { Services = homeBefore.Services }) == JsonSerializer.Serialize(homeBefore), "All earlier Home content and shared collections unchanged.");
        Check(JsonSerializer.Serialize((await page.GetAsync()) with { Services = pageBefore.Services }) == JsonSerializer.Serialize(pageBefore), "Services editorial sections unchanged.");

        var unavailable = new ServiceContentService(new UnavailableFactory(), auth, options, NullLogger<ServiceContentService>.Instance);
        Check((await unavailable.GetAsync()).SequenceEqual(initial) && (await unavailable.GetHomeFeaturedAsync()).SequenceEqual(initialFeatured), "Read failure returns exact approved full/featured defaults.");
        await ExpectAsync<SqliteException>(() => unavailable.SaveHomeFeaturedAsync([]), "Failed featured write propagates without fake save.");
        await ExpectAsync<SqliteException>(() => unavailable.CreateAsync(model), "Failed create propagates.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Services.Where(x => x.Id == rows[0].Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.IconPath, "../unsafe.png"));
        Check((await service.GetAsync()).SequenceEqual(initial) && (await service.GetHomeFeaturedAsync()).SequenceEqual(initialFeatured), "Invalid stored icons cause safe read fallback, never arbitrary rendering.");
        foreach (var row in await service.ListAsync()) await service.DeleteAsync(row.Id);
        Check((await service.GetAsync()).Count == 0 && (await service.GetHomeFeaturedAsync()).Count == 0, "Valid empty collections do not use defaults.");
        foreach (var route in new[] { "/", "/services" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(!html.Contains("class=\"service_box\""), "Zero Service state renders no empty/broken cards.");
        }
        await service.ReorderAsync([]); await service.SaveHomeFeaturedAsync([]);
        await CheckEditorAsync();
        await CheckFeaturedEditorAsync();
    }

    private static async Task CheckFeaturedEditorAsync()
    {
        var editor = new HomeFeaturedServicesEditor();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = editor.GetType(); var stub = new EditorStub();
        type.GetProperty("ServicesService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<HomeFeaturedServicesEditor>.Instance);
        async Task Invoke(string name) => await (Task)type.GetMethod(name, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        await Invoke("OnInitializedAsync"); Check(!Dirty(), "Featured load clean.");
        type.GetMethod("Toggle", flags)!.Invoke(editor, [2, true]); Check(Dirty(), "Staged membership is dirty.");
        type.GetMethod("Toggle", flags)!.Invoke(editor, [2, false]); Check(!Dirty(), "Reverting selection is clean.");
        type.GetMethod("Toggle", flags)!.Invoke(editor, [2, true]);
        await Invoke("SaveAsync"); Check(!Dirty(), "Featured save clean.");
        type.GetMethod("Move", flags)!.Invoke(editor, [2, -1]); Check(Dirty(), "Staged featured reorder is dirty.");
        stub.Failure = new ValidationException("Internal");
        await Invoke("SaveAsync"); Check(Dirty(), "Featured failed save retains staged order.");
        stub.Failure = null; await Invoke("SaveAsync"); Check(!Dirty(), "Featured successful reorder save clean.");
        type.GetMethod("Toggle", flags)!.Invoke(editor, [1, false]);
        type.GetMethod("Toggle", flags)!.Invoke(editor, [2, false]); Check(Dirty(), "Clear selection is dirty.");
        await Invoke("SaveAsync"); Check(!Dirty(), "Clear selection saves clean.");
    }

    private static async Task CheckEditorAsync()
    {
        var editor = new ServiceEditor();
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var stub = new EditorStub();
        type.GetProperty("ServicesService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<ServiceEditor>.Instance);
        async Task Invoke(string name) => await (Task)type.GetMethod(name, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        foreach (int? id in new int?[] { null, 1 })
        {
            type.GetProperty("Id", flags)!.SetValue(editor, id);
            await Invoke("OnParametersSetAsync");
            Check(!Dirty(), "New/edit form starts clean.");
            var model = (ServiceEditModel)type.GetField("_model", flags)!.GetValue(editor)!;
            foreach (var field in typeof(ServiceEditModel).GetProperties())
            {
                var original = field.GetValue(model);
                field.SetValue(model, field.PropertyType == typeof(bool) ? !(bool)original! : "Changed"); Check(Dirty(), "Every shared field participates in dirty tracking.");
                field.SetValue(model, original); Check(!Dirty(), "Revert clears shared editor dirty state.");
            }
            model.Name = "Test Service"; model.Tagline = "Test tagline"; model.Description = "Test description."; model.IconPath = ServiceIconAssets.Paths[0];
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

    private sealed class EditorStub : IServiceContentService
    {
        public Exception? Failure { get; set; }
        public Task<IReadOnlyList<ServiceSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveHomeFeaturedAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        public Task<IReadOnlyList<ServiceSummary>> GetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ServiceListItem>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ServiceListItem>>([new(1, 1, "First", "Tag", ServiceIconAssets.Paths[0], 1), new(2, 2, "Second", "Tag", ServiceIconAssets.Paths[1], null)]);
        public Task<ServiceEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(new ServiceEditModel { Name = "Original Service", Tagline = "Original tagline", Description = "Original description.", IconPath = ServiceIconAssets.Paths[0] });
        public Task<int> CreateAsync(ServiceEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.FromResult(3) : Task.FromException<int>(Failure);
        public Task UpdateAsync(int id, ServiceEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
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
