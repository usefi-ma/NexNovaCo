using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class ProjectMediaChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword(), publicBaseUrl: "https://media.example.invalid");
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        var paths = app.Services.GetRequiredService<MediaFilePaths>();
        var defaults = app.Services.GetRequiredService<ProjectCatalog>();
        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
        var auth = new TestAuth(await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin));
        var media = new LocalMediaStorageService(paths, factory, auth, options);
        var service = new ProjectContentService(factory, auth, options, NullLogger<ProjectContentService>.Instance, defaults, media);
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var jpg = await File.ReadAllBytesAsync(Path.Combine(root, ProjectImageAssets.Paths[0]));
        var png = await File.ReadAllBytesAsync(Path.Combine(root, PartnerLogoAssets.Paths[0]));
        var webp = Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEADsD+JaQAA3AAAAAA");
        var uploads = new List<string>();
        foreach (var (bytes, extension) in new[] { (jpg,"jpg"), (png,"png"), (webp,"webp"), (jpg,"jpg"), (jpg,"jpg") })
        {
            var upload = await media.ReadAsync(new TestFile("image." + extension, MediaPolicy.ContentType("x." + extension), bytes), MediaKind.Project);
            uploads.Add(await media.SaveAsync(upload));
            Check(MediaPolicy.IsGenerated(uploads[^1], MediaKind.Project), "Project upload uses controlled GUID path.");
            Check((await client.GetAsync("/" + uploads[^1])).StatusCode == HttpStatusCode.OK, "Project upload is publicly readable raster.");
        }
        Check(MediaPolicy.MaxBytes(MediaKind.Project) == 3 * 1024 * 1024, "Project reuses existing entity image size policy.");
        var model = ProjectEditModel.FromContent((await defaults.GetDetailsAsync())[0]);
        model.Slug = "media-workflow"; model.Name = "Media workflow"; model.ImagePath = uploads[4];
        model.Gallery = uploads.Take(4).Select((path, i) => new ProjectGalleryEditRow { Source = path, Alt = "Gallery " + (char)('A' + i) }).ToList();
        (model.Gallery[0], model.Gallery[3]) = (model.Gallery[3], model.Gallery[0]);
        var id = await service.CreateAsync(model);
        await service.SaveHomeFeaturedAsync([id]);
        await VerifyAsync(model.Gallery.Select(x => x.Source).ToArray());
        Check((await service.GetHomeFeaturedAsync()).Single().ImagePath == uploads[4], "Home uses explicit cover, not reordered first gallery image.");
        Check((await service.GetAsync()).Single(x => x.Slug == model.Slug).ImagePath == uploads[4], "Listing uses canonical cover.");
        var html = await client.GetStringAsync("/projects/" + model.Slug);
        Check(html.Contains("https://media.example.invalid/" + uploads[4]), "OG image uses uploaded canonical cover.");
        Check(html.Contains("https://media.example.invalid/projects/media-workflow") && html.Contains(model.Name) && html.Contains(model.Description), "Project SEO title/description/canonical intact.");
        Check(html.Contains("Next project image"), "Multi-image gallery retains controls.");
        await VerifyRestartAsync(model.Gallery.Select(x => x.Source).ToArray());

        model.Gallery.RemoveAt(1);
        await service.UpdateAsync(id, model);
        await VerifyAsync(model.Gallery.Select(x => x.Source).ToArray());
        Check(uploads.All(path => File.Exists(paths.PhysicalPath(path))), "Removing a gallery relation does not delete any physical file.");
        foreach (var count in new[] { 0, 1, 2, 4, 5 })
        {
            model.Gallery = uploads.Take(count).Select((path, i) => new ProjectGalleryEditRow { Source = path, Alt = "Image " + (i + 1) }).ToList();
            await service.UpdateAsync(id, model);
            await VerifyAsync(model.Gallery.Select(x => x.Source).ToArray());
            var page = await client.GetStringAsync("/projects/" + model.Slug);
            Check(page.Contains("aria-roledescription=\"carousel\"") == (count > 0), "Zero hides gallery; nonempty gallery renders.");
            Check(page.Contains("Next project image") == (count > 1), "Only multiple images have gallery controls.");
            if (count == 0) await VerifyRestartAsync([]);
        }
        var original = model.Gallery[0].Source;
        foreach (var invalid in new[] { "../secret.jpg", "image/project/missing.jpg", "uploads/projects/" + new string('a',32) + ".svg", "uploads/team/" + new string('a',32) + ".jpg", "uploads/projects/" + Guid.NewGuid().ToString("N") + ".jpg" })
        {
            model.Gallery[0].Source = invalid;
            await RejectAsync<ValidationException>(() => service.UpdateAsync(id, model));
            model.Gallery[0].Source = original;
            var cover = model.ImagePath; model.ImagePath = invalid;
            await RejectAsync<ValidationException>(() => service.UpdateAsync(id, model));
            model.ImagePath = cover;
        }
        var alt = model.Gallery[0].Alt; model.Gallery[0].Alt = " ";
        await RejectAsync<ValidationException>(() => service.UpdateAsync(id, model));
        model.Gallery[0].Alt = alt;
        var before = await service.GetForEditAsync(id);
        Check(before.Gallery.Count == 5 && before.ImagePath == uploads[4], "Rejected edits leave all canonical media unchanged.");
        var missing = "uploads/projects/" + Guid.NewGuid().ToString("N") + ".png";
        await using (var db = await factory.CreateDbContextAsync())
        {
            var entity = await db.Projects.Include(x => x.Gallery).SingleAsync(x => x.Id == id);
            entity.ImagePath = missing; entity.Gallery.ForEach(x => x.Source = missing);
            await db.SaveChangesAsync();
            Check(!db.Database.HasPendingModelChanges(), "Existing schema supports project media without a migration.");
        }
        var unavailable = (await service.GetDetailAsync(model.Slug))!;
        Check(unavailable.Gallery.Count == 0 && unavailable.Summary.ImagePath == MediaPolicy.Fallback(MediaKind.Project), "Missing uploads hide gallery and safely resolve cover.");
        Check(!(await client.GetStringAsync("/projects/" + model.Slug)).Contains(missing), "Public markup never emits missing upload URLs.");
        Check((await service.GetForEditAsync(id)).ImagePath == missing, "Public fallback does not rewrite stored paths.");
        await service.UpdateAsync(id, model);
        await VerifyRestartAsync(model.Gallery.Select(x => x.Source).ToArray());
        var pending = await media.ReadAsync(new TestFile("a.jpg","image/jpeg",jpg), MediaKind.Project);
        foreach (var user in new[] { new ClaimsPrincipal(new ClaimsIdentity()), new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, admin.Id)], "test")) })
        {
            auth.User = user;
            await RejectAsync<UnauthorizedAccessException>(() => media.ReadAsync(new TestFile("a.jpg","image/jpeg",jpg), MediaKind.Project));
            await RejectAsync<UnauthorizedAccessException>(() => media.SaveAsync(pending));
            await RejectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(id, model));
        }

        async Task VerifyAsync(string[] order)
        {
            Check((await service.GetForEditAsync(id)).Gallery.Select(x => x.Source).SequenceEqual(order), "Editor refresh preserves exact gallery order.");
            Check((await service.GetDetailAsync(model.Slug))!.Gallery.Select(x => x.Source).SequenceEqual(order), "Public gallery uses exact canonical order.");
        }
        async Task VerifyRestartAsync(string[] order)
        {
            await using var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath);
            using var restartedClient = restarted.NewClient();
            await using var restartedScope = restarted.Services.CreateAsyncScope();
            var read = (await restartedScope.ServiceProvider.GetRequiredService<IProjectCatalog>().GetDetailAsync(model.Slug))!;
            Check(read.Gallery.Select(x => x.Source).SequenceEqual(order), "Restart preserves exact uploaded order or intentionally empty gallery.");
            Check(read.Summary.ImagePath == model.ImagePath, "Restart preserves distinct cover.");
        }
    }
    private static async Task RejectAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { Check(true, "Invalid or unauthorized project media rejected."); return; }
        Check(false, "Expected " + typeof(T).Name);
    }
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public ClaimsPrincipal User { get; set; } = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
    }
    private sealed class TestFile(string name, string type, byte[] bytes) : IBrowserFile
    {
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => type;
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream(bytes, false);
    }
}
