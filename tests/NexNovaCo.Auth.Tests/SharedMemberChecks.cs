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

internal static class SharedMemberChecks
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
        foreach (var route in new[] { "/dashboard/content/shared-team", "/dashboard/content/shared-team/new", "/dashboard/content/shared-team/1", "/dashboard/content/shared-team/home-featured" })
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Anonymous shared management route challenges.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin shared management route denied.");
            foreach (var attempt in Enumerable.Range(0, 2))
                Check((await admin.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Admin route supports bookmarks/refresh.");
        }
        Check((await admin.GetStringAsync("/dashboard/content/shared-team/2147483647")).Contains("no longer exists"), "Missing edit route gives safe feedback.");
        Check((await admin.GetStringAsync("/dashboard/content/shared-team")).Contains("Shared Content") &&
            (await admin.GetStringAsync("/dashboard/content/home/team")).Contains("Team Section"), "Shared collection and Home presentation navigation are distinct.");

        var defaults = app.Services.GetRequiredService<MemberCatalog>();
        var details = await defaults.GetDetailsAsync();
        var auth = new TestAuth(principal);
        var service = new MemberContentService(factory, auth, options, NullLogger<MemberContentService>.Instance, defaults);
        var initial = await service.GetAsync();
        var initialFeatured = await service.GetHomeFeaturedAsync();
        Check(initial.SequenceEqual(await defaults.GetAsync()), "Exact original canonical Member identities/order retained.");
        Check(initial.All(x => MemberImageAssets.IsAllowed(x.ImagePath)), "Approved member covers retained.");
        Check(initialFeatured.SequenceEqual(initial.Take(4)), "Original Home subset and independent order seeded exactly.");
        var rows = await service.ListAsync();
        Check(rows.Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Shared explicit order.");
        Check(rows.Take(4).Select(x => x.HomeDisplayOrder).SequenceEqual(Enumerable.Range(1, 4).Select(x => (int?)x)) && rows[^1].HomeDisplayOrder is null, "Admin list shows correct Home membership/order.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            await MemberInitializer.InitializeAsync(db, defaults); await MemberInitializer.InitializeAsync(db, defaults);
            Check(await db.Members.CountAsync() == 6 && await db.HomeFeaturedMembers.CountAsync() == 4 &&
                await db.MemberInitializationStates.CountAsync() == 1 && !db.Database.HasPendingModelChanges(), "Idempotent atomic seed and migration model agree.");
        }
        await using var publicScope = app.Services.CreateAsyncScope();
        var home = publicScope.ServiceProvider.GetRequiredService<IHomeContentService>();
        var page = publicScope.ServiceProvider.GetRequiredService<ITeamContentService>();
        var homeBefore = await home.GetAsync();
        var pageBefore = await page.GetAsync();
        async Task CheckPublicAsync()
        {
            Check(JsonSerializer.Serialize((await page.GetAsync()).Members) == JsonSerializer.Serialize(await service.GetAsync()), "Members page reads fresh full database content.");
            Check(JsonSerializer.Serialize((await home.GetAsync()).Members) == JsonSerializer.Serialize(await service.GetHomeFeaturedAsync()), "Home reads fresh joined featured content.");
        }
        var model = MemberEditModel.FromContent(details[0]);
        model.Slug = "qa-member"; model.Name = "QA Member";
        var id = await service.CreateAsync(model);
        var created = (await service.GetAsync())[^1];
        Check((await service.ListAsync())[^1] is { DisplayOrder: 7, HomeDisplayOrder: null }, "New Member appends without auto-featuring.");
        Check((await anonymous.GetStringAsync("/team")).Contains(model.Name) && !(await anonymous.GetStringAsync("/")).Contains(model.Name), "Create appears only on full catalog before selection.");
        var featuredIds = new[] { id, rows[1].Id, rows[0].Id };
        await service.SaveHomeFeaturedAsync(featuredIds);
        Check((await service.GetHomeFeaturedAsync()).Select(x => x.Name).SequenceEqual(new[] { model.Name, initial[1].Name, initial[0].Name }), "Featured save adds/removes/joins canonical entities in Home order.");
        Check((await anonymous.GetStringAsync("/")).Contains(model.Name), "Selected Member appears on Home.");
        model.Name = "QA Edited Member"; model.Role = "Edited role";
        await service.UpdateAsync(id, model);
        Check((await service.GetAsync())[^1].Slug == created.Slug && (await service.GetHomeFeaturedAsync())[0].Name == model.Name, "Edit retains identity and changes both consumers without copies.");
        var featuredBefore = JsonSerializer.Serialize(await service.GetHomeFeaturedAsync());
        var order = (await service.ListAsync()).Select(x => x.Id).Reverse().ToArray();
        await service.ReorderAsync(order);
        Check((await service.ListAsync()).Select(x => x.Id).SequenceEqual(order), "Shared reorder persists.");
        Check(JsonSerializer.Serialize(await service.GetHomeFeaturedAsync()) == featuredBefore, "Shared order never changes Home order.");
        var sharedBefore = JsonSerializer.Serialize(await service.GetAsync());
        await service.SaveHomeFeaturedAsync(featuredIds.Reverse().ToArray());
        Check(JsonSerializer.Serialize(await service.GetAsync()) == sharedBefore, "Featured reorder never changes Members page order.");
        Check((await service.GetHomeFeaturedAsync()).Select(x => x.Name).SequenceEqual(new[] { initial[0].Name, initial[1].Name, model.Name }), "Featured reorder persisted independently.");
        await CheckPublicAsync();
        Check(JsonSerializer.Serialize(await service.GetDetailAsync(model.Slug)) == JsonSerializer.Serialize((await defaults.GetDetailAsync("emilyjohnson"))! with { Summary = (await service.GetAsync()).Single(x => x.Slug == model.Slug) }), "Created detail retains exact approved child content.");
        model.Skills.Reverse(); model.Biography = "Edited biography."; model.Role = "Edited role";
        model.LinkedIn = "https://www.linkedin.com/in/test"; model.Telegram = "https://t.me/test"; model.Email = "test@example.invalid";
        model.Skills.Add(new() { Text = "Added skill" });
        await service.UpdateAsync(id, model);
        var detail = (await service.GetDetailAsync(model.Slug))!;
        Check(detail.Biography == model.Biography && detail.Skills.SequenceEqual(model.Skills.Select(x => x.Text)) &&
            detail.Summary.Role == model.Role && detail.Summary.LinkedIn == model.LinkedIn && detail.Summary.Telegram == model.Telegram && detail.Summary.Email == model.Email, "Detail skills, role and supported contact destinations update.");
        model.Skills.RemoveAt(0); model.Telegram = ""; model.Email = null;
        await service.UpdateAsync(id, model);
        Check((await service.GetDetailAsync(model.Slug))!.Skills.Count == model.Skills.Count &&
            (await service.GetDetailAsync(model.Slug))!.Summary.Telegram == "", "Removing skill and optional links persists.");
        var oldSlug = model.Slug; model.Slug = "  QA-CHANGED  ";
        await service.UpdateAsync(id, model); model.Slug = "qa-changed";
        Check(await service.GetDetailAsync(oldSlug) is null && await service.GetDetailAsync(model.Slug) is not null, "Slug normalized; previous URL safely missing without redirect history.");
        Check((await anonymous.GetAsync("/team/" + oldSlug)).StatusCode == HttpStatusCode.NotFound &&
            (await anonymous.GetAsync("/team/" + model.Slug)).StatusCode == HttpStatusCode.OK, "Detail routes follow live canonical slug.");
        var duplicate = MemberEditModel.FromContent(details[0]);
        await ExpectAsync<ValidationException>(() => service.CreateAsync(duplicate), "Duplicate normalized slug rejected.");
        duplicate.Slug = "EMILYJOHNSON";
        await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, duplicate), "Case-insensitive duplicate slug rejected.");
        foreach (var slug in new[] { "../unsafe", "has space", "/dashboard", "a--b", "a?b", "a%2fb", "a_b" })
        {
            duplicate.Slug = slug;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(duplicate), "Unsafe slug rejected.");
            Check(await service.GetDetailAsync(slug) is null, "Malformed detail slug safely missing.");
        }
        foreach (var invalidChild in new[] { true, false })
        {
            var invalid = MemberEditModel.FromContent(details[0]); invalid.Slug = "invalid-child";
            if (invalidChild) invalid.ImagePath = "../unsafe.jpg"; else invalid.Skills[0].Text = " ";
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Nested content is validated at service boundary.");
        }

        foreach (var url in new[] { "javascript:alert(1)", "data:text/html,test", "http://example.com", "//example.com", "https://user:pass@example.com", "https://example.com/a b", "https://example.com/\\\\bad", "https://example.com/\n" })
        {
            var invalid = MemberEditModel.FromContent(details[0]); invalid.Slug = "invalid-url";
            invalid.LinkedIn = url;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Unsafe LinkedIn rejected.");
            invalid.LinkedIn = ""; invalid.Telegram = url;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Unsafe Telegram rejected.");
        }
        foreach (var email in new[] { "mailto:test@example.com", "test@example.com?bcc=other@example.com", "test@example.com%0d%0a", "not-an-email", "a@example.com,b@example.com" })
        {
            var invalid = MemberEditModel.FromContent(details[0]); invalid.Email = email;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Unsafe email rejected.");
        }
        var listBefore = JsonSerializer.Serialize(await service.ListAsync());
        foreach (var invalid in new[] { new[] { id, id }, new[] { int.MaxValue }, Array.Empty<int>() })
            await ExpectAsync<ValidationException>(() => service.ReorderAsync(invalid), "Incomplete/duplicate/stale shared reorder rejected.");
        foreach (var invalid in new[] { new[] { id, id }, new[] { int.MaxValue } })
            await ExpectAsync<ValidationException>(() => service.SaveHomeFeaturedAsync(invalid), "Duplicate/missing featured selection rejected.");
        Check(JsonSerializer.Serialize(await service.ListAsync()) == listBefore, "Rejected operations do not partially write either order.");
        foreach (var property in typeof(MemberEditModel).GetProperties().Where(x => x.GetCustomAttribute<RequiredAttribute>() is not null))
        {
            var invalid = MemberEditModel.FromContent(details[0]);
            property.SetValue(invalid, "   ");
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Required field enforced on create.");
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Required field enforced on update.");
            if (property.GetCustomAttribute<StringLengthAttribute>() is not { } length) continue;
            property.SetValue(invalid, new string('x', length.MaximumLength + 1));
            await ExpectAsync<ValidationException>(() => service.UpdateAsync(id, invalid), "Member field length enforced.");
        }
        foreach (var icon in new[] { "../secret.png", "C:/secret.png", "/image/service/icons/software.png", "https://example.com/icon.png", "image/service/icons/%2e%2e/x.png", "image/service/icons/missing.png" })
        {
            var invalid = MemberEditModel.FromContent(details[0]); invalid.ImagePath = icon;
            await ExpectAsync<ValidationException>(() => service.CreateAsync(invalid), "Only approved icon paths accepted.");
        }
        model.Introduction = "<script>alert(1)</script>";
        await service.UpdateAsync(id, model);
        foreach (var route in new[] { "/", "/team" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(html.Contains("&lt;script&gt;") && !html.Contains(model.Introduction), "Shared text is HTML encoded.");
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
        await ExpectAsync<KeyNotFoundException>(() => service.UpdateAsync(id, model), "Deleted Member cannot be recreated by stale edit.");
        await ExpectAsync<KeyNotFoundException>(() => service.GetForEditAsync(id), "Missing Member editor safe.");
        await using (var db = await factory.CreateDbContextAsync())
            Check(!await db.MemberSkills.AnyAsync(x => x.MemberId == id) && !await db.HomeFeaturedMembers.AnyAsync(x => x.MemberId == id) && await db.HomeFeaturedMembers.CountAsync() == 2, "Delete cascades selection and preserves others.");
        Check((await service.ListAsync()).Select(x => x.DisplayOrder).SequenceEqual(Enumerable.Range(1, 6)), "Delete normalizes shared positions.");
        Check((await service.ListAsync()).Where(x => x.HomeDisplayOrder != null).OrderBy(x => x.HomeDisplayOrder).Select(x => x.HomeDisplayOrder).SequenceEqual(new int?[] { 1, 2 }), "Delete normalizes featured positions.");
        await CheckPublicAsync();
        foreach (var route in new[] { "/", "/team" }) Check(!(await anonymous.GetStringAsync(route)).Contains(model.Name), "Delete removed both public occurrences.");
        Check(JsonSerializer.Serialize((await home.GetAsync()) with { Members = homeBefore.Members }) == JsonSerializer.Serialize(homeBefore), "All earlier Home content and shared collections unchanged.");
        Check(JsonSerializer.Serialize((await page.GetAsync()) with { Members = pageBefore.Members }) == JsonSerializer.Serialize(pageBefore), "Team editorial sections unchanged.");

        var unavailable = new MemberContentService(new UnavailableFactory(), auth, options, NullLogger<MemberContentService>.Instance, defaults);
        Check((await unavailable.GetAsync()).SequenceEqual(initial) && (await unavailable.GetHomeFeaturedAsync()).SequenceEqual(initialFeatured), "Read failure returns exact approved full/featured defaults.");
        Check(JsonSerializer.Serialize(await unavailable.GetDetailAsync("emilyjohnson")) == JsonSerializer.Serialize(details[0]), "Detail read failure returns approved detail without writes.");
        await ExpectAsync<SqliteException>(() => unavailable.SaveHomeFeaturedAsync([]), "Failed featured write propagates without fake save.");
        await ExpectAsync<SqliteException>(() => unavailable.CreateAsync(model), "Failed create propagates.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Members.Where(x => x.Id == rows[0].Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.ImagePath, "../unsafe.png"));
        Check((await service.GetAsync()).SequenceEqual(initial) && (await service.GetHomeFeaturedAsync()).SequenceEqual(initialFeatured), "Invalid stored icons cause safe read fallback, never arbitrary rendering.");
        foreach (var row in await service.ListAsync()) await service.DeleteAsync(row.Id);
        Check((await service.GetAsync()).Count == 0 && (await service.GetHomeFeaturedAsync()).Count == 0, "Valid empty collections do not use defaults.");
        foreach (var route in new[] { "/", "/team" })
        {
            var html = await anonymous.GetStringAsync(route);
            Check(!html.Contains("class=\"team_member_box\""), "Zero Member state renders no empty/broken cards.");
        }
        await service.ReorderAsync([]); await service.SaveHomeFeaturedAsync([]);
        await CheckEditorAsync();
        await CheckFeaturedEditorAsync();
    }

    private static async Task CheckFeaturedEditorAsync()
    {
        var editor = new HomeFeaturedMembersEditor();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = editor.GetType(); var stub = new EditorStub();
        type.GetProperty("MembersService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<HomeFeaturedMembersEditor>.Instance);
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
        var editor = new MemberEditor();
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var stub = new EditorStub();
        type.GetProperty("MembersService", flags)!.SetValue(editor, stub);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<MemberEditor>.Instance);
        async Task Invoke(string name) => await (Task)type.GetMethod(name, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        foreach (int? id in new int?[] { null, 1 })
        {
            type.GetProperty("Id", flags)!.SetValue(editor, id);
            await Invoke("OnParametersSetAsync");
            Check(!Dirty(), "New/edit form starts clean.");
            var model = (MemberEditModel)type.GetField("_model", flags)!.GetValue(editor)!;
            foreach (var field in typeof(MemberEditModel).GetProperties().Where(x => x.PropertyType == typeof(string)))
            {
                var original = field.GetValue(model);
                field.SetValue(model, field.PropertyType == typeof(bool) ? !(bool)original! : "Changed"); Check(Dirty(), "Every shared field participates in dirty tracking.");
                field.SetValue(model, original); Check(!Dirty(), "Revert clears shared editor dirty state.");
            }
            model.Skills.Add(new() { Text = "Skill A" }); model.Skills.Add(new() { Text = "Skill B" });
            await Invoke("SaveAsync"); Check(!Dirty(), "Child-only save establishes a clean snapshot.");
            model.Skills[0].Text = "Edited skill"; Check(Dirty(), "Skill text changes are dirty.");
            model.Skills[0].Text = "Skill A"; Check(!Dirty(), "Skill text revert is clean.");
            model.Skills.Reverse(); Check(Dirty(), "Skill reordering is dirty.");
            model.Skills.Reverse(); Check(!Dirty(), "Skill ordering revert is clean.");
            model.Skills.RemoveAt(0); Check(Dirty(), "Child removal is dirty.");
            model.Slug = "test-member"; model.Biography = "Full description";
            model.Skills.Add(new() { Text = "Unsaved skill" });
            Check(Dirty(), "Child addition participates in dirty tracking.");
            model.Name = "Test Member"; model.Role = "Test role"; model.Introduction = "Test description."; model.ImagePath = MemberImageAssets.Paths[0];
            type.GetMethod("ClearSaved", flags)!.Invoke(editor, null);
            Check(Dirty(), "Invalid submit retains dirty state.");
            foreach (var failure in new Exception[] { new DbUpdateException("Technical detail"), new KeyNotFoundException("Technical detail"), new ValidationException("Technical detail") })
            {
                stub.Failure = failure;
                await Invoke("SaveAsync");
                Check(Dirty() && model.Introduction == "Test description.", "Failed create/edit retains unsaved content.");
                if (failure is not ValidationException) Check(!((string)type.GetField("_error", flags)!.GetValue(editor)!).Contains("Technical"), "Safe database error does not expose exception detail.");
            }
            stub.Failure = null;
            await Invoke("SaveAsync");
            Check(!Dirty() && (bool)type.GetField("_saved", flags)!.GetValue(editor)!, "Successful create/edit establishes clean state.");
        }
    }

    private sealed class EditorStub : IMemberContentService
    {
        public Exception? Failure { get; set; }
        public Task<MemberDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TeamMemberSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveHomeFeaturedAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        public Task<IReadOnlyList<TeamMemberSummary>> GetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<MemberListItem>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MemberListItem>>([new(1, 1, "First", "first", "Role", MemberImageAssets.Paths[0], 1), new(2, 2, "Second", "second", "Role", MemberImageAssets.Paths[1], null)]);
        public Task<MemberEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(new MemberEditModel { Slug = "original-member", Biography = "Original full detail", Name = "Original Member", Role = "Original role", Introduction = "Original description.", ImagePath = MemberImageAssets.Paths[0] });
        public Task<int> CreateAsync(MemberEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.FromResult(3) : Task.FromException<int>(Failure);
        public Task UpdateAsync(int id, MemberEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
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
