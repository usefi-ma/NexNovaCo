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

internal static class SharedTestimonialChecks
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
        foreach (var route in new[] { "/dashboard/content/testimonials", "/dashboard/content/testimonials/new", "/dashboard/content/testimonials/1" })
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous shared management route challenges.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin shared management route denied.");
            foreach (var attempt in Enumerable.Range(0, 2))
                Check((await admin.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Admin route supports bookmarks/refresh.");
        }
        Check((await admin.GetStringAsync("/dashboard/content/testimonials/2147483647")).Contains("no longer exists"), "Missing edit route gives safe feedback.");
        Check((await admin.GetStringAsync("/dashboard/content/testimonials")).Contains("Shared Content") &&
            (await admin.GetStringAsync("/dashboard/content/home/testimonials")).Contains("Testimonials Section"), "Shared collection and Home presentation navigation are distinct.");

        var auth = new TestAuth(principal);
        var service = new TestimonialContentService(factory, auth, options, NullLogger<TestimonialContentService>.Instance);
        var baseline = await service.GetAsync();
        Check(baseline.Count == 2 && baseline[0].Attribution == "Olivia Carter, COO at Alpha Co" && baseline[1].Attribution == "Daniel Kim, Marketing Director at Tech Co" && baseline.All(x => x.Paragraphs.Count == 2), "Seed keeps exact approved count, attribution, paragraph structure and order.");
        var initialRows = await service.ListAsync();
        Check(initialRows.Select(x => x.DisplayOrder).SequenceEqual([1, 2]), "Initial explicit ordering.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            await TestimonialInitializer.InitializeAsync(db);
            await TestimonialInitializer.InitializeAsync(db);
            Check(await db.Testimonials.CountAsync() == 2 && !db.Database.HasPendingModelChanges(), "Idempotent initializer and migration model agreement.");
        }

        await using var publicScope = app.Services.CreateAsyncScope();
        var home = publicScope.ServiceProvider.GetRequiredService<IHomeContentService>();
        var projects = publicScope.ServiceProvider.GetRequiredService<IProjectsContentService>();
        var homeBefore = await home.GetAsync();
        var projectsBefore = await projects.GetAsync();
        async Task CheckSharedAsync(string message)
        {
            var expected = await service.GetAsync();
            Check(JsonSerializer.Serialize((await home.GetAsync()).Testimonials) == JsonSerializer.Serialize(expected) &&
                JsonSerializer.Serialize((await projects.GetAsync()).Testimonials) == JsonSerializer.Serialize(expected), message);
        }
        var model = new TestimonialEditModel { Attribution = "QA Author, Lead at QA Co", Quote = "First QA paragraph.\r\n\r\nSecond QA paragraph." };
        var id = await service.CreateAsync(model);
        Check(id > 0 && !initialRows.Any(x => x.Id == id), "Create uses a new generated identity.");
        var afterCreate = await service.ListAsync();
        Check(afterCreate.Count == 3 && afterCreate[^1].Id == id && afterCreate[^1].DisplayOrder == 3, "Create appends and persists.");
        Check((await service.GetAsync())[^1].Paragraphs.SequenceEqual(new[] { "First QA paragraph.", "Second QA paragraph." }), "Blank-line quote editor preserves ordered paragraphs.");
        await CheckSharedAsync("Existing scoped public providers read newly created DB content without stale snapshots.");
        foreach (var route in new[] { "/", "/projects" })
            Check((await anonymous.GetStringAsync(route)).Contains("QA Author"), "Created testimonial renders on both anonymous pages.");

        model.Attribution = "QA Edited Author";
        model.Quote = "Edited shared quote.\n\nAnother paragraph.";
        await service.UpdateAsync(id, model);
        Check((await service.ListAsync())[^1] is { DisplayOrder: 3, Attribution: "QA Edited Author" }, "Edit persists without changing order/identity.");
        var order = afterCreate.Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(order);
        Check((await service.ListAsync()).Select(x => x.Id).SequenceEqual(order), "Explicit reorder persists.");
        await CheckSharedAsync("Home and Projects consume identical reordered/edited SQLite content.");
        foreach (var route in new[] { "/", "/projects" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(html.IndexOf("QA Edited Author", StringComparison.Ordinal) < html.IndexOf("Daniel Kim, Marketing", StringComparison.Ordinal), "Public rendered order reflects database.");
        }
        await using (var db = await factory.CreateDbContextAsync()) await TestimonialInitializer.InitializeAsync(db);
        Check((await service.ListAsync())[0].Attribution == model.Attribution, "Startup initializer preserves Admin edits and order.");
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var client = restarted.NewClient();
            var html = await client.GetStringAsync("/projects");
            Check(html.Contains("Edited shared quote.") && html.IndexOf("QA Edited Author", StringComparison.Ordinal) < html.IndexOf("Daniel Kim, Marketing", StringComparison.Ordinal), "Restart preserves added/edited/reordered content.");
            Check((await Login(client, AuthFactory.Email, app.Password)).StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther, "Shared migration/startup preserves existing Identity password.");
        }

        var orderBefore = JsonSerializer.Serialize(await service.ListAsync());
        foreach (var invalidIds in new[] { new[] { id, id, id }, new[] { id }, new[] { id, -1, int.MaxValue } })
            await ExpectAsync<ValidationException>(() => service.ReorderAsync(invalidIds), "Malformed/stale reorder rejected atomically.");
        Check(JsonSerializer.Serialize(await service.ListAsync()) == orderBefore, "Rejected reorder leaves all data/order untouched.");
        foreach (var property in typeof(TestimonialEditModel).GetProperties())
        {
            var invalid = TestimonialEditModel.FromContent(baseline[0]);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Required field rejects whitespace on create.");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Required field rejects whitespace on update.");
            property.SetValue(invalid, new string('x', property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Field max length enforced in service.");
        }
        model.Quote = "<script>alert(1)</script>";
        await service.UpdateAsync(id, model);
        foreach (var route in new[] { "/", "/projects" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(html.Contains("&lt;script&gt;") && !html.Contains(model.Quote), "Public quotes remain encoded plain text.");
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
            Check((await service.GetAsync()).Count == 3, "Public read remains anonymous.");
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
        Check((await service.ListAsync()).Select(x => x.DisplayOrder).SequenceEqual([1, 2]), "Delete normalizes remaining order.");
        await CheckSharedAsync("Both public pages reflect deletion.");
        foreach (var route in new[] { "/", "/projects" }) Check(!(await anonymous.GetStringAsync(route)).Contains("QA Edited Author"), "Deleted testimonial absent publicly.");
        Check((await home.GetAsync()).TestimonialBrand == homeBefore.TestimonialBrand && (await projects.GetAsync()).TestimonialBrand == projectsBefore.TestimonialBrand, "Shared CRUD leaves page-specific brand panels untouched.");
        var homeAfter = await home.GetAsync();
        Check(JsonSerializer.Serialize(homeAfter with { Testimonials = homeBefore.Testimonials }) == JsonSerializer.Serialize(homeBefore), "All eight Home settings and unrelated catalogs survive shared CRUD.");

        var unavailable = new TestimonialContentService(new UnavailableFactory(), auth, options, NullLogger<TestimonialContentService>.Instance);
        Check(JsonSerializer.Serialize(await unavailable.GetAsync()) == JsonSerializer.Serialize(baseline), "Unavailable DB returns exact approved fallback with no writes.");
        await ExpectAsync<SqliteException>(() => unavailable.ListAsync(), "Admin list must not hide database failures using defaults.");
        await ExpectAsync<SqliteException>(() => unavailable.CreateAsync(model), "Write failure propagates for safe editor feedback.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Testimonials.Where(x => x.Id == initialRows[0].Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Quote, ""));
        Check(JsonSerializer.Serialize(await service.GetAsync()) == JsonSerializer.Serialize(baseline), "Invalid stored content safely falls back.");
        // Destructive scenarios are confined to this factory's disposable isolated DB.
        foreach (var row in await service.ListAsync()) await service.DeleteAsync(row.Id);
        Check((await service.GetAsync()).Count == 0, "Empty collection is respected, not replaced by read fallback.");
        foreach (var route in new[] { "/", "/projects" })
            Check(!(await anonymous.GetStringAsync(route)).Contains("data-carousel-kind=\"testimonials\""), "Zero-item state does not mount an empty Owl carousel.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.Testimonials.CountAsync() == 0, "Empty public read performs no hidden seeding.");
            await TestimonialInitializer.InitializeAsync(db);
        }
        Check(JsonSerializer.Serialize(await service.GetAsync()) == JsonSerializer.Serialize(baseline), "Controlled startup reseeds only a fully empty collection.");
        await CheckEditorAsync();
    }

    private static async Task CheckEditorAsync()
    {
        var editor = new TestimonialEditor();
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var stub = new EditorStub();
        type.GetProperty("TestimonialsService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<TestimonialEditor>.Instance);
        async Task Invoke(string name) => await (Task)type.GetMethod(name, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        foreach (int? id in new int?[] { null, 1 })
        {
            type.GetProperty("Id", flags)!.SetValue(editor, id);
            await Invoke("OnParametersSetAsync");
            Check(!Dirty(), "New/edit form starts clean.");
            var model = (TestimonialEditModel)type.GetField("_model", flags)!.GetValue(editor)!;
            foreach (var field in typeof(TestimonialEditModel).GetProperties())
            {
                var original = field.GetValue(model);
                field.SetValue(model, "Changed"); Check(Dirty(), "Every shared field participates in dirty tracking.");
                field.SetValue(model, original); Check(!Dirty(), "Revert clears shared editor dirty state.");
            }
            model.Attribution = "Test Author"; model.Quote = "Test quote.";
            type.GetMethod("ClearSaved", flags)!.Invoke(editor, null);
            Check(Dirty(), "Invalid submit retains dirty state.");
            foreach (var failure in new Exception[] { new DbUpdateException("Technical detail"), new KeyNotFoundException("Technical detail"), new ValidationException("Technical detail") })
            {
                stub.Failure = failure;
                await Invoke("SaveAsync");
                Check(Dirty() && model.Quote == "Test quote.", "Failed create/edit retains unsaved content.");
                Check(!((string)type.GetField("_error", flags)!.GetValue(editor)!).Contains("Technical"), "Safe editor error does not expose exception detail.");
            }
            stub.Failure = null;
            await Invoke("SaveAsync");
            Check(!Dirty() && (bool)type.GetField("_saved", flags)!.GetValue(editor)!, "Successful create/edit establishes clean state.");
        }
    }

    private sealed class EditorStub : ITestimonialContentService
    {
        public Exception? Failure { get; set; }
        public Task<IReadOnlyList<Testimonial>> GetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TestimonialListItem>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TestimonialEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(new TestimonialEditModel { Attribution = "Original Author", Quote = "Original quote." });
        public Task<int> CreateAsync(TestimonialEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.FromResult(3) : Task.FromException<int>(Failure);
        public Task UpdateAsync(int id, TestimonialEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
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
