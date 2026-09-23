using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class MediaChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        var paths = app.Services.GetRequiredService<MediaFilePaths>();
        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
        var principal = await signIn.CreateUserPrincipalAsync(admin);
        var auth = new TestAuth(principal);
        var media = new LocalMediaStorageService(paths, factory, auth, options);
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var jpg = await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.HeroDefault));
        var png = await File.ReadAllBytesAsync(Path.Combine(root, PartnerLogoAssets.Paths[0]));
        // One-pixel raster fixture, no new package or runtime image editing.
        var webp = Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEADsD+JaQAA3AAAAAA");
        var uploaded = new Dictionary<MediaKind, string>();
        foreach (var kind in Enum.GetValues<MediaKind>())
        {
            var bytes = kind == MediaKind.Partner ? png : kind == MediaKind.Member ? webp : jpg;
            var extension = kind == MediaKind.Partner ? ".png" : kind == MediaKind.Member ? ".webp" : ".jpg";
            var selected = await media.ReadAsync(new TestFile("untrusted-name" + extension, MediaPolicy.ContentType("a" + extension), bytes), kind);
            Check(!Directory.Exists(paths.Root) || !Directory.EnumerateFiles(paths.Root, "*", SearchOption.AllDirectories).Any(x => x.Contains("untrusted-name")), "Selection never persists a user filename.");
            var saved = await media.SaveAsync(selected);
            uploaded[kind] = saved;
            Check(MediaPolicy.IsGenerated(saved, kind) && !saved.Contains("untrusted") && File.Exists(paths.PhysicalPath(saved)), "Generated GUID path uses only the controlled kind folder.");
            Check((await File.ReadAllBytesAsync(paths.PhysicalPath(saved))).SequenceEqual(bytes), "Exact validated image bytes saved without editing.");
            var response = await client.GetAsync("/" + saved);
            Check(response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentType!.MediaType == MediaPolicy.ContentType(saved), "Anonymous raster GET uses fixed server content type.");
            Check(response.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "Runtime media is served with nosniff.");
            Check((await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/" + saved))).StatusCode == HttpStatusCode.OK, "HEAD supported for runtime media.");
            var post = await client.PostAsync("/" + saved, new ByteArrayContent(bytes));
            Check(post.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound or HttpStatusCode.BadRequest, $"No unauthenticated HTTP upload endpoint ({post.StatusCode}).");
            Check((await File.ReadAllBytesAsync(paths.PhysicalPath(saved))).SequenceEqual(bytes), "Rejected POST cannot overwrite an upload.");
            Check((await media.SaveAsync(selected)) != saved, "Repeated upload creates a unique file without overwriting.");
        }
        Check((await File.ReadAllBytesAsync(Path.Combine(root, MediaPolicy.HeroDefault))).SequenceEqual(jpg), "Bundled source image never overwritten.");
        Check(!Directory.EnumerateFiles(paths.Root, "*.tmp", SearchOption.AllDirectories).Any(), "Atomic persistence leaves no temporary files.");
        var jpeg = await media.ReadAsync(new TestFile("photo.JPEG", "image/jpeg", jpg), MediaKind.Hero);
        Check((await media.SaveAsync(jpeg)).EndsWith(".jpg"), "JPEG extension normalized on server.");
        foreach (var name in new[] { "../a.jpg", "..\\a.jpg", "C:\\secret.jpg", "/tmp/a.jpg", "a/portrait.jpg", "a.jpg:evil", "a.jpg\n", "a.svg", "a.html", "a.jpg.exe" })
            await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile(name, "image/jpeg", jpg), MediaKind.Hero), "Reject unsafe filename/extension: " + name);
        await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("a.png", "image/png", jpg), MediaKind.Hero), "Fake extension rejected.");
        await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("a.jpg", "text/html", jpg), MediaKind.Hero), "Fake MIME rejected.");
        foreach (var extension in new[] { ".jpg", ".png", ".webp" })
        {
            await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("x" + extension, MediaPolicy.ContentType("x" + extension), "<script>alert(1)</script>"u8.ToArray()), MediaKind.Hero), "HTML/script disguised as raster rejected.");
            var bytes = extension == ".jpg" ? jpg : extension == ".png" ? png : webp;
            await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("x" + extension, MediaPolicy.ContentType("x" + extension), bytes[..20]), MediaKind.Hero), "Truncated format rejected.");
        }
        foreach (var kind in Enum.GetValues<MediaKind>())
        {
            await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("a.jpg", "image/jpeg", jpg, MediaPolicy.MaxBytes(kind) + 1), kind), "Server limit rejects oversized metadata.");
            await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("a.jpg", "image/jpeg", new byte[MediaPolicy.MaxBytes(kind) + 1], 1), kind), "Actual streamed bytes cannot bypass size limit.");
        }
        await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("a.jpg", "image/jpeg", jpg, jpg.Length - 1), MediaKind.Hero), "Size mismatch rejected.");
        var corruptPng = png.ToArray(); corruptPng[29] ^= 1;
        await RejectAsync<ValidationException>(() => media.ReadAsync(new TestFile("a.png", "image/png", corruptPng), MediaKind.Partner), "PNG CRC mismatch rejected.");
        foreach (var path in new[] { "../secret", "C:/secret.jpg", "/image/home/header.jpg", "uploads/home/../../a.jpg", "uploads/team/123.jpg", "uploads/team/" + new string('a',32) + ".svg" })
        {
            Check(!MediaPolicy.IsGenerated(path), "Noncanonical local path rejected.");
            await RejectAsync<ArgumentException>(() => Task.FromResult(paths.PhysicalPath(path)), "Filesystem API never resolves arbitrary paths.");
            Check(media.ResolvePublicPath(path, MediaKind.Hero) == MediaPolicy.HeroDefault, "Unsafe path uses public fallback.");
        }
        await RejectAsync<ValidationException>(() => Task.Run(() => media.RequireAvailable(uploaded[MediaKind.Partner], MediaKind.Member)), "Cross-folder path rejected by entity validation.");
        foreach (var kind in Enum.GetValues<MediaKind>())
        {
            var missing = $"uploads/{MediaPolicy.Folder(kind)}/{Guid.NewGuid():N}.png";
            Check(media.ResolvePublicPath(missing,kind) == MediaPolicy.Fallback(kind), "Missing file resolves to approved fallback.");
            await RejectAsync<ValidationException>(() => Task.Run(() => media.RequireAvailable(missing,kind)), "Cannot save nonexistent upload reference.");
        }
        // Canonical entity integration, persistence, missing-file fallback and rollback semantics.
        var hero = new HomeHeroContentService(factory,auth,options,NullLogger<HomeHeroContentService>.Instance,media);
        var welcome = new HomeWelcomeContentService(factory,auth,options,NullLogger<HomeWelcomeContentService>.Instance,media);
        var members = new MemberContentService(factory,auth,options,NullLogger<MemberContentService>.Instance,app.Services.GetRequiredService<MemberCatalog>(),media);
        var partners = new PartnerContentService(factory,auth,options,NullLogger<PartnerContentService>.Instance,media);
        var h = await hero.GetForEditAsync(); var w = await welcome.GetForEditAsync();
        var memberId = (await members.ListAsync())[0].Id; var partnerId = (await partners.ListAsync())[0].Id;
        var m = await members.GetForEditAsync(memberId); var p = await partners.GetForEditAsync(partnerId);
        h.ImagePath = uploaded[MediaKind.Hero]; w.ImagePath = uploaded[MediaKind.Welcome];
        m.ImagePath = uploaded[MediaKind.Member]; p.ImagePath = uploaded[MediaKind.Partner];
        await hero.UpdateAsync(h); await welcome.UpdateAsync(w); await members.UpdateAsync(memberId,m); await partners.UpdateAsync(partnerId,p);
        Check((await hero.GetAsync()).ImagePath == h.ImagePath && (await welcome.GetAsync()).ImagePath == w.ImagePath, "Home settings return canonical saved upload paths.");
        Check((await members.GetAsync())[0].ImagePath == m.ImagePath && (await members.GetHomeFeaturedAsync())[0].ImagePath == m.ImagePath && (await members.GetDetailAsync(m.Slug))!.Summary.ImagePath == m.ImagePath, "One Member path serves listing, Home featured and detail.");
        foreach (var (route, requiredPaths) in new (string,string[])[] { ("/",[h.ImagePath,w.ImagePath,m.ImagePath,p.ImagePath]), ("/team",[m.ImagePath]), ("/team/" + m.Slug,[m.ImagePath]), ("/about",[p.ImagePath]) })
        {
            var response = await client.GetAsync(route); var html = await response.Content.ReadAsStringAsync();
            Check(response.StatusCode == HttpStatusCode.OK && requiredPaths.All(html.Contains), "Affected public HTML renders saved uploads: " + route);
            if (route == "/")
                Check(html.Contains("https://localhost/" + h.ImagePath) && html.Contains("https://localhost/" + w.ImagePath), "Home CSS image URLs must be absolute so stylesheet-relative resolution cannot break them.");
        }
        await using (var restarted = new AuthFactory(NewPassword(), databasePath: app.DatabasePath))
        {
            using var restartedClient = restarted.NewClient();
            Check((await restartedClient.GetStringAsync("/")).Contains(h.ImagePath) && (await restartedClient.GetAsync("/" + p.ImagePath)).IsSuccessStatusCode, "Restart preserves DB references and runtime files.");
        }
        var next = await media.SaveAsync(jpeg);
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER BlockHeroSave BEFORE UPDATE ON HomeHeroSettings BEGIN SELECT RAISE(ABORT, 'isolated save failure'); END;");
        h.ImagePath = next;
        await RejectAsync<DbUpdateException>(() => hero.UpdateAsync(h), "Entity save failure is surfaced after file persistence.");
        Check((await hero.GetForEditAsync()).ImagePath == uploaded[MediaKind.Hero] && File.Exists(paths.PhysicalPath(uploaded[MediaKind.Hero])), "Failed DB save preserves previous DB reference and file.");
        Check(File.Exists(paths.PhysicalPath(next)), "Safe orphan retained for future cleanup.");
        await using (var db = await factory.CreateDbContextAsync())
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockHeroSave;");
        // Delete only generated files in this test's isolated root, never a source or normal upload.
        foreach (var path in uploaded.Values) File.Delete(paths.PhysicalPath(path));
        Check((await hero.GetAsync()).ImagePath == MediaPolicy.HeroDefault && (await welcome.GetAsync()).ImagePath == MediaPolicy.WelcomeDefault, "Home missing-upload fallback retains text.");
        Check((await members.GetAsync())[0].ImagePath == MediaPolicy.MemberFallback && (await members.GetDetailAsync(m.Slug))!.Summary.ImagePath == MediaPolicy.MemberFallback, "Member missing file falls back consistently.");
        Check((await partners.GetAsync())[0].ImagePath == PartnerLogoAssets.Paths[0], "Partner missing-file fallback.");
        Check((await hero.GetForEditAsync()).ImagePath == uploaded[MediaKind.Hero], "Fallback never rewrites saved DB path.");
        foreach (var route in new[] { "/", "/team", "/team/" + m.Slug, "/about" })
            Check((await client.GetAsync(route)).IsSuccessStatusCode, "Missing uploads do not break public pages.");
        foreach (var kind in Enum.GetValues<MediaKind>())
            Check(File.Exists(Path.Combine(root,MediaPolicy.Fallback(kind))), "Every public fallback is an existing source asset.");

        await MediaEditorChecks.RunAsync(factory, media, hero, welcome, members, partners, jpg, png, memberId, partnerId);
        var countBefore = Directory.GetFiles(paths.Root,"*",SearchOption.AllDirectories).Length;
        auth.User = new ClaimsPrincipal(new ClaimsIdentity());
        await RejectAsync<UnauthorizedAccessException>(() => media.ReadAsync(new TestFile("a.jpg","image/jpeg",jpg),MediaKind.Hero), "Anonymous service read denied.");
        await RejectAsync<UnauthorizedAccessException>(() => media.SaveAsync(jpeg), "Anonymous service write denied.");
        auth.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,admin.Id)],"test"));
        await RejectAsync<UnauthorizedAccessException>(() => media.SaveAsync(jpeg), "Non-Admin service write denied.");
        auth.User = principal;
        await users.RemoveFromRoleAsync(admin,"Admin");
        await RejectAsync<UnauthorizedAccessException>(() => media.SaveAsync(jpeg), "Revoked live role rejects a previously selected file.");
        await users.AddToRoleAsync(admin,"Admin");
        await users.UpdateSecurityStampAsync(admin);
        await RejectAsync<UnauthorizedAccessException>(() => media.SaveAsync(jpeg), "Revoked security stamp rejects a previously selected file.");
        Check(Directory.GetFiles(paths.Root,"*",SearchOption.AllDirectories).Length == countBefore, "Rejected authorization never writes files.");
        await CheckMigrationAsync();
    }

    private static async Task CheckMigrationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(connection));
        await using var provider = services.BuildServiceProvider();
        await using var db = await provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260923015219_AddSharedMembersAndHomeFeatured");
        await HistoricalHomeFixture.InitializeAsync(db);
        await db.Database.ExecuteSqlRawAsync("UPDATE HomeHeroSettings SET OpeningLine='Preserved before media';");
        var before = await HistoricalHomeFixture.SnapshotAsync(db);
        await migrator.MigrateAsync();
        Check(await HistoricalHomeFixture.SnapshotAsync(db) == before, "Media migration preserves all existing Home content/timestamps.");
        Check((await db.HomeHeroSettings.SingleAsync()).ImagePath == MediaPolicy.HeroDefault && (await db.HomeWelcomeSettings.SingleAsync()).ImagePath == MediaPolicy.WelcomeDefault, "Existing settings gain exact approved image defaults.");
        Check(!db.Database.HasPendingModelChanges(), "Media migration matches model.");
    }
    private static async Task RejectAsync<T>(Func<Task> action,string message) where T : Exception
    {
        try { await action(); } catch (T) { Check(true,message); return; }
        Check(false,message);
    }
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public ClaimsPrincipal User { get; set; } = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
    }
    private sealed class TestFile(string name,string type,byte[] bytes,long? claimedSize = null) : IBrowserFile
    {
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => claimedSize ?? bytes.Length;
        public string ContentType => type;
        public Stream OpenReadStream(long maxAllowedSize = 512000,CancellationToken cancellationToken = default) => new MemoryStream(bytes,false);
    }
}
