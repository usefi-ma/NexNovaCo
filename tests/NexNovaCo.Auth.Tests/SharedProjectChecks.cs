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

internal static class SharedProjectChecks
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
        foreach (var route in new[] { "/dashboard/content/shared-projects", "/dashboard/content/shared-projects/new", "/dashboard/content/shared-projects/1", "/dashboard/content/shared-projects/home-featured" })
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous shared management route challenges.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin shared management route denied.");
            foreach (var attempt in Enumerable.Range(0, 2))
                Check((await admin.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Admin route supports bookmarks/refresh.");
        }
        Check((await admin.GetStringAsync("/dashboard/content/shared-projects/2147483647")).Contains("no longer exists"), "Missing edit route gives safe feedback.");
        Check((await admin.GetStringAsync("/dashboard/content/shared-projects")).Contains("Shared Content") &&
            (await admin.GetStringAsync("/dashboard/content/home/projects")).Contains("Projects Section"), "Shared collection and Home presentation navigation are distinct.");

        var defaults = app.Services.GetRequiredService<ProjectCatalog>();
        var details = await defaults.GetDetailsAsync();
        var auth = new TestAuth(principal);
        var service = new ProjectContentService(factory, auth, options, NullLogger<ProjectContentService>.Instance, defaults);
        var initial = await service.GetAsync();
        var initialFeatured = await service.GetHomeFeaturedAsync();
        Check(initial.SequenceEqual(await defaults.GetAsync()), "Exact original canonical Project identities/order retained.");
        Check(initial.All(x => ProjectImageAssets.IsAllowed(x.ImagePath)), "Approved project covers retained.");
        Check(initialFeatured.SequenceEqual(initial.Take(5)), "Original Home subset and independent order seeded exactly.");
        var rows = await service.ListAsync();
        Check(rows.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 7)), "Shared explicit order.");
        Check(rows.Take(5).Select(x => x.HomeDisplayOrder).SequenceEqual(Enumerable.Range(1, 5).Select(x => (int?)x)) && rows[^1].HomeDisplayOrder is null, "Admin list shows correct Home membership/order.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            await ProjectInitializer.InitializeAsync(db, defaults); await ProjectInitializer.InitializeAsync(db, defaults);
            Check(await db.Projects.CountAsync() == 7 && await db.HomeFeaturedProjects.CountAsync() == 5 &&
                await db.ProjectInitializationStates.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Idempotent atomic seed and migration model agree.");
        }
        await using var publicScope = app.Services.CreateAsyncScope();
        var home = publicScope.ServiceProvider.GetRequiredService<IHomeContentService>();
        var page = publicScope.ServiceProvider.GetRequiredService<IProjectsContentService>();
        var homeBefore = await home.GetAsync();
        var pageBefore = await page.GetAsync();
        async Task CheckPublicAsync()
        {
            Check(JsonSerializer.Serialize((await page.GetAsync()).Projects) == JsonSerializer.Serialize(await service.GetAsync()), "Projects page reads fresh full database content.");
            Check(JsonSerializer.Serialize((await home.GetAsync()).Projects) == JsonSerializer.Serialize(await service.GetHomeFeaturedAsync()), "Home reads fresh joined featured content.");
        }
        var model = ProjectEditModel.FromContent(details[0]);
        model.Slug = "qa-project"; model.Name = "QA Project";
        var id = await service.CreateAsync(model);
        var created = (await service.GetAsync())[^1];
        Check((await service.ListAsync())[^1] is { DisplayOrder: 8, HomeDisplayOrder: null }, "New Project appends without auto-featuring.");
        Check((await anonymous.GetStringAsync("/projects")).Contains(model.Name) && !(await anonymous.GetStringAsync("/")).Contains(model.Name), "Create appears only on full catalog before selection.");
        var featuredIds = new[] { id, rows[1].Id, rows[0].Id };
        await service.SaveHomeFeaturedAsync(featuredIds);
        Check((await service.GetHomeFeaturedAsync()).Select(x => x.Name).SequenceEqual(new[] { model.Name, initial[1].Name, initial[0].Name }), "Featured save adds/removes/joins canonical entities in Home order.");
        Check((await anonymous.GetStringAsync("/")).Contains(model.Name), "Selected Project appears on Home.");
        model.Name = "QA Edited Project"; model.Tagline = "Edited tagline";
        await service.UpdateAsync(id, model);
        Check((await service.GetAsync())[^1].Slug == created.Slug && (await service.GetHomeFeaturedAsync())[0].Name == model.Name, "Edit retains identity and changes both consumers without copies.");
        var featuredBefore = JsonSerializer.Serialize(await service.GetHomeFeaturedAsync());
        var order = (await service.ListAsync()).Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(order);
        Check((await service.ListAsync()).Select(x => x.Id).SequenceEqual(order), "Shared reorder persists.");
        Check(JsonSerializer.Serialize(await service.GetHomeFeaturedAsync()) == featuredBefore, "Shared order never changes Home order.");
        var sharedBefore = JsonSerializer.Serialize(await service.GetAsync());
        await service.SaveHomeFeaturedAsync(featuredIds.Reverse().ToArray());
        Check(JsonSerializer.Serialize(await service.GetAsync()) == sharedBefore, "Featured reorder never changes Projects page order.");
        Check((await service.GetHomeFeaturedAsync()).Select(x => x.Name).SequenceEqual(new[] { initial[0].Name, initial[1].Name, model.Name }), "Featured reorder persisted independently.");
        await CheckPublicAsync();
        Check(JsonSerializer.Serialize(await service.GetDetailAsync(model.Slug)) == JsonSerializer.Serialize((await defaults.GetDetailAsync("nexconnect"))! with { Summary = (await service.GetAsync()).Single(x => x.Slug == model.Slug) }), "Created detail retains exact approved child content.");
        model.Gallery.Reverse(); model.Features.Reverse(); model.FullDescription = "Edited full detail.";
        model.Technologies = "Typed metadata"; model.Gallery.Add(new() { Source = ProjectImageAssets.Paths[1], Alt = "Added gallery image" });
        model.Features.Add(new() { Text = "Added feature" });
        await service.UpdateAsync(id, model);
        var detail = (await service.GetDetailAsync(model.Slug))!;
        Check(detail.FullDescription == model.FullDescription && detail.Gallery.Select(x => x.Alt).SequenceEqual(model.Gallery.Select(x => x.Alt)) &&
            detail.Features.SequenceEqual(model.Features.Select(x => x.Text)) && detail.Metadata.Technologies == model.Technologies, "Detail children/content update and retain explicit order.");
        model.Gallery.RemoveAt(0); model.Features.RemoveAt(0);
        await service.UpdateAsync(id, model);
        Check((await service.GetDetailAsync(model.Slug))!.Gallery.Count == model.Gallery.Count, "Removing child rows does not retain obsolete images.");
        var oldSlug = model.Slug; model.Slug = "  QA-CHANGED  ";
        await service.UpdateAsync(id, model); model.Slug = "qa-changed";
        Check(await service.GetDetailAsync(oldSlug) is null && await service.GetDetailAsync(model.Slug) is not null, "Slug normalized; previous URL safely missing without redirect history.");
        Check((await anonymous.GetAsync("/projects/" + oldSlug)).StatusCode == HttpStatusCode.NotFound &&
            (await anonymous.GetAsync("/projects/" + model.Slug)).StatusCode == HttpStatusCode.OK, "Detail routes follow live canonical slug.");
        var duplicate = ProjectEditModel.FromContent(details[0]);
        await ExpectAsync<ValidationException>(() => service.CreateAsync(duplicate), "Duplicate normalized slug rejected.");
        duplicate.Slug = "NEXCONNECT";
        await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, duplicate), "Case-insensitive duplicate slug rejected.");
        foreach (var slug in new[] { "../unsafe", "has space", "/dashboard", "a--b", "a?b", "a%2fb", "a_b" })
        {
            duplicate.Slug = slug;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(duplicate), "Unsafe slug rejected.");
            Check(await service.GetDetailAsync(slug) is null, "Malformed detail slug safely missing.");
        }
        foreach (var invalidChild in new[] { true, false })
        {
            var invalid = ProjectEditModel.FromContent(details[0]); invalid.Slug = "invalid-child";
            if (invalidChild) invalid.Gallery[0].Source = "../unsafe.jpg"; else invalid.Features[0].Text = " ";
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Nested content is validated at service boundary.");
        }

        var listBefore = JsonSerializer.Serialize(await service.ListAsync());
        foreach (var invalid in new[] { new[] { id, id }, new[] { int.MaxValue }, Array.Empty<int>() })
            await ExpectAsync<ValidationException>(() => service.ReorderAsync(invalid), "Incomplete/duplicate/stale shared reorder rejected.");
        foreach (var invalid in new[] { new[] { id, id }, new[] { int.MaxValue } })
            await ExpectAsync<ValidationException>(() => service.SaveHomeFeaturedAsync(invalid), "Duplicate/missing/over-capacity featured selection rejected.");
        Check(JsonSerializer.Serialize(await service.ListAsync()) == listBefore, "Rejected operations do not partially write either order.");
        foreach (var property in typeof(ProjectEditModel).GetProperties().Where(x => x.GetCustomAttribute<RequiredAttribute>() is not null))
        {
            var invalid = ProjectEditModel.FromContent(details[0]);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Required field enforced on create.");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Required field enforced on update.");
            if (property.GetCustomAttribute<StringLengthAttribute>() is not { } length) continue;
            property.SetValue(invalid, new string('x', length.MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Project field length enforced.");
        }
        foreach (var icon in new[] { "../secret.png", "C:/secret.png", "/image/service/icons/software.png", "https://example.com/icon.png", "image/service/icons/%2e%2e/x.png", "image/service/icons/missing.png" })
        {
            var invalid = ProjectEditModel.FromContent(details[0]); invalid.ImagePath = icon;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Only approved icon paths accepted.");
        }
        model.Description = "<script>alert(1)</script>";
        await service.UpdateAsync(id, model);
        foreach (var route in new[] { "/", "/projects" })
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
            Check((await service.GetAsync()).Count == 8 && (await service.GetHomeFeaturedAsync()).Count == 3, "Both public reads remain anonymous.");
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
        await ExpectAsync<KeyNotFoundException>(() => service.UpdateAsync(id, model), "Deleted Project cannot be recreated by stale edit.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(id), "Missing Project editor safe.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(!await db.ProjectGalleryImages.AnyAsync(x => x.ProjectId == id) && !await db.ProjectFeatures.AnyAsync(x => x.ProjectId == id) && !await db.HomeFeaturedProjects.AnyAsync(x => x.ProjectId == id) && await db.HomeFeaturedProjects.CountAsync() == 2, "Delete cascades selection and preserves others.");
        Check((await service.ListAsync()).Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 7)), "Delete normalizes shared positions.");
        Check((await service.ListAsync()).Where(x => x.HomeDisplayOrder != null).OrderBy(x => x.HomeDisplayOrder).Select(x => x.HomeDisplayOrder).SequenceEqual(new int?[] { 1, 2 }), "Delete normalizes featured positions.");
        await CheckPublicAsync();
        foreach (var route in new[] { "/", "/projects" }) Check(!(await anonymous.GetStringAsync(route)).Contains(model.Name), "Delete removed both public occurrences.");
        Check(JsonSerializer.Serialize((await home.GetAsync()) with { Projects = homeBefore.Projects }) == JsonSerializer.Serialize(homeBefore), "All earlier Home content and shared collections unchanged.");
        Check(JsonSerializer.Serialize((await page.GetAsync()) with { Projects = pageBefore.Projects }) == JsonSerializer.Serialize(pageBefore), "Services editorial sections unchanged.");

        var unavailable = new ProjectContentService(new UnavailableFactory(), auth, options, NullLogger<ProjectContentService>.Instance, defaults);
        Check((await unavailable.GetAsync()).SequenceEqual(initial) && (await unavailable.GetHomeFeaturedAsync()).SequenceEqual(initialFeatured), "Read failure returns exact approved full/featured defaults.");
        Check(JsonSerializer.Serialize(await unavailable.GetDetailAsync("nexconnect")) == JsonSerializer.Serialize(details[0]), "Detail read failure returns approved detail without writes.");
        await ExpectAsync<SqliteException>(() => unavailable.SaveHomeFeaturedAsync([]), "Failed featured write propagates without fake save.");
        await ExpectAsync<SqliteException>(() => unavailable.CreateAsync(model), "Failed create propagates.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Projects.Where(x => x.Id == rows[0].Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.ImagePath, "../unsafe.png"));
        Check((await service.GetAsync()).SequenceEqual(initial) && (await service.GetHomeFeaturedAsync()).SequenceEqual(initialFeatured), "Invalid stored icons cause safe read fallback, never arbitrary rendering.");
        foreach (var row in await service.ListAsync()) await service.DeleteAsync(row.Id);
        Check((await service.GetAsync()).Count == 0 && (await service.GetHomeFeaturedAsync()).Count == 0, "Valid empty collections do not use defaults.");
        foreach (var route in new[] { "/", "/projects" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(!html.Contains("class=\"project_img\""), "Zero Project state renders no empty/broken cards.");
        }
        await service.ReorderAsync([]); await service.SaveHomeFeaturedAsync([]);
        await CheckEditorAsync();
        await CheckFeaturedEditorAsync();
    }

    private static async Task CheckFeaturedEditorAsync()
    {
        var editor = new HomeFeaturedProjectsEditor();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = editor.GetType(); var stub = new EditorStub();
        type.GetProperty("ProjectsService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<HomeFeaturedProjectsEditor>.Instance);
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
        var editor = new ProjectEditor();
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var stub = new EditorStub();
        type.GetProperty("ProjectsService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<ProjectEditor>.Instance);
        async Task Invoke(string name) => await (Task)type.GetMethod(name, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        foreach (int? id in new int?[] { null, 1 })
        {
            type.GetProperty("Id", flags)!.SetValue(editor, id);
            await Invoke("OnParametersSetAsync");
            Check(!Dirty(), "New/edit form starts clean.");
            var model = (ProjectEditModel)type.GetField("_model", flags)!.GetValue(editor)!;
            foreach (var field in typeof(ProjectEditModel).GetProperties().Where(x => x.PropertyType == typeof(string)))
            {
                var original = field.GetValue(model);
                field.SetValue(model, field.PropertyType == typeof(bool) ? !(bool)original! : "Changed"); Check(Dirty(), "Every shared field participates in dirty tracking.");
                field.SetValue(model, original); Check(!Dirty(), "Revert clears shared editor dirty state.");
            }
            model.Gallery.Add(new() { Source = ProjectImageAssets.Paths[0], Alt = "Gallery A" });
            Check(Dirty(), "Adding a gallery row is dirty even without top-level edits.");
            model.Gallery.Clear(); Check(!Dirty(), "Reverting gallery additions is clean.");
            model.Features.Add(new() { Text = "Feature A" }); model.Features.Add(new() { Text = "Feature B" });
            await Invoke("SaveAsync"); Check(!Dirty(), "Child-only save establishes a clean snapshot.");
            model.Features[0].Text = "Edited feature"; Check(Dirty(), "Feature text changes are dirty.");
            model.Features[0].Text = "Feature A"; Check(!Dirty(), "Feature text revert is clean.");
            model.Features.Reverse(); Check(Dirty(), "Feature reordering is dirty.");
            model.Features.Reverse(); Check(!Dirty(), "Feature ordering revert is clean.");
            model.Features.RemoveAt(0); Check(Dirty(), "Child removal is dirty.");
            model.Slug = "test-project"; model.FullDescription = "Full description";
            model.Gallery.Add(new() { Source = ProjectImageAssets.Paths[0], Alt = "Unsaved image" });
            Check(Dirty(), "Child addition participates in dirty tracking.");
            model.Name = "Test Project"; model.Tagline = "Test tagline"; model.Description = "Test description."; model.ImagePath = ProjectImageAssets.Paths[0];
            type.GetMethod("ClearSaved", flags)!.Invoke(editor, null);
            Check(Dirty(), "Invalid submit retains dirty state.");
            foreach (var failure in new Exception[] { new DbUpdateException("Technical detail"), new KeyNotFoundException("Technical detail"), new ValidationException("Technical detail") })
            {
                stub.Failure = failure;
                await Invoke("SaveAsync");
                Check(Dirty() && model.Description == "Test description.", "Failed create/edit retains unsaved content.");
                if (failure is not ValidationException) Check(!((string)type.GetField("_error", flags)!.GetValue(editor)!).Contains("Technical"), "Safe database error does not expose exception detail.");
            }
            stub.Failure = null;
            await Invoke("SaveAsync");
            Check(!Dirty() && (bool)type.GetField("_saved", flags)!.GetValue(editor)!, "Successful create/edit establishes clean state.");
        }
    }

    private sealed class EditorStub : IProjectContentService
    {
        public Exception? Failure { get; set; }
        public Task<ProjectDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProjectSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveHomeFeaturedAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        public Task<IReadOnlyList<ProjectSummary>> GetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProjectListItem>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProjectListItem>>([new(1, 1, "First", "Tag", ProjectImageAssets.Paths[0], 1), new(2, 2, "Second", "Tag", ProjectImageAssets.Paths[1], null)]);
        public Task<ProjectEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(new ProjectEditModel { Slug = "original-project", FullDescription = "Original full detail", Name = "Original Project", Tagline = "Original tagline", Description = "Original description.", ImagePath = ProjectImageAssets.Paths[0] });
        public Task<int> CreateAsync(ProjectEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.FromResult(3) : Task.FromException<int>(Failure);
        public Task UpdateAsync(int id, ProjectEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
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
