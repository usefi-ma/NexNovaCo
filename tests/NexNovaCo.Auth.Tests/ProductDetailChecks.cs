using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class ProductDetailChecks
{
    private const string Origin = "https://products.example.invalid";

    public static async Task RunAsync()
    {
        await MigrationUpgradeAsync();
        await using var app = new AuthFactory(NewPassword(), publicBaseUrl: Origin);
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        var paths = app.Services.GetRequiredService<MediaFilePaths>();

        await FreshDetailAsync(app, client, factory);

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
        var auth = new TestAuth(principal);
        var media = new LocalMediaStorageService(paths, factory, auth, options);
        var products = new ProductContentService(factory, auth, options, media, NullLogger<ProductContentService>.Instance);

        await DetailCrudAsync(app, client, factory, media, products);
        await RelationValidationAsync(products);
        await AuthorizationAsync(factory, options, paths);
    }

    private static async Task FreshDetailAsync(AuthFactory app, HttpClient client, IDbContextFactory<ApplicationDbContext> factory)
    {
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.ProductDetailInitializationStates.CountAsync() == 1, "Fresh database stores one Product Detail initialization marker.");
            Check(await db.Products.CountAsync() == 6, "Phase 23 preserves the six Phase 22 Products.");
            Check(await db.Products.AllAsync(x => x.FullDescription != ""), "Fresh seeded Products receive detail descriptions.");
            Check(await db.ProductGalleryImages.CountAsync() == 18 && await db.ProductFeatures.CountAsync() == 18 && await db.ProductRelatedProducts.CountAsync() == 18,
                "Fresh detail initialization provides ordered gallery, feature and related demo content.");
            Check(!db.Database.HasPendingModelChanges(), "Product Detail runtime model matches its migration snapshot.");
        }

        var response = await client.GetAsync("/shop/productivity-pro");
        var html = await response.Content.ReadAsStringAsync();
        Check(response.StatusCode == HttpStatusCode.OK && html.Contains("About this product") && html.Contains("Productivity workspace overview"), "Valid Product slug renders full public detail content.");
        Check(html.Contains("href=\"/\">Home</a>") && html.Contains("href=\"/shop\"") && html.Contains("Productivity Pro"), "Product Detail renders semantic linked Home / Shop / Product breadcrumb content.");
        Check(html.Contains("product-gallery__control--previous") && html.Contains("aria-roledescription=\"carousel\"") && html.Contains("aria-live=\"polite\""), "Multi-image gallery exposes accessible controls and status.");
        Check(html.Contains("Related products") && Count(html, "shop-product-card\"") == 3, "Product Detail renders exactly three explicitly related ProductCards.");
        Check(html.Contains($"<link rel=\"canonical\" href=\"{Origin}/shop/productivity-pro\"") && html.Contains("property=\"og:title\"") && html.Contains("name=\"twitter:title\""), "Product Detail uses shared canonical, Open Graph and Twitter metadata.");
        Check(Count(html, "<h1") == 1 && html.Contains("Current price:") && html.Contains("Original price:"), "Product Detail has one H1 and accessible current/original price meaning.");
        Check(!html.Contains("Add to Cart", StringComparison.OrdinalIgnoreCase) && !html.Contains("Buy Now", StringComparison.OrdinalIgnoreCase) && html.Contains("Catalog preview only"), "Product Detail never implies unavailable purchasing behavior.");
        Check((await client.GetAsync("/shop/not-a-real-product")).StatusCode == HttpStatusCode.NotFound, "Invalid Product slug returns safe not-found behavior.");
        Check((await client.GetStringAsync("/shop")).Contains("href=\"/shop/productivity-pro\""), "Shop ProductCard CTA targets Product Detail.");
        Check((await client.GetStringAsync("/sitemap.xml")).Contains($"{Origin}/shop/productivity-pro"), "Sitemap includes current Product Detail URLs.");

        await using var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: Origin);
        using var restartedClient = restarted.NewClient();
        Check((await restartedClient.GetAsync("/shop/productivity-pro")).StatusCode == HttpStatusCode.OK, "Product Detail remains available after restart.");
        await using var restartedScope = restarted.Services.CreateAsyncScope();
        var restartedDb = restartedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Check(await restartedDb.ProductGalleryImages.CountAsync() == 18 && await restartedDb.ProductDetailInitializationStates.CountAsync() == 1,
            "Restart does not duplicate Product detail child collections.");
    }

    private static async Task DetailCrudAsync(AuthFactory app, HttpClient client, IDbContextFactory<ApplicationDbContext> factory,
        LocalMediaStorageService media, ProductContentService products)
    {
        var target = (await products.ListAsync()).First();
        var related = (await products.ListChoicesAsync(target.Id)).Take(3).ToArray();
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, ProductImageAssets.Paths[0]));
        var uploaded = new List<string>();
        foreach (var name in new[] { "gallery-a.jpg", "gallery-b.jpg", "gallery-c.jpg", "gallery-d.jpg" })
        {
            var pending = await media.ReadAsync(new TestFile(name, "image/jpeg", bytes), MediaKind.Product);
            uploaded.Add(await media.SaveAsync(pending));
        }
        Check(uploaded.Distinct().Count() == 4 && uploaded.All(path => path.StartsWith("uploads/products/", StringComparison.Ordinal)), "Gallery A/B/C/D use secure GUID-named Product uploads.");

        var model = await products.GetForEditAsync(target.Id);
        model.FullDescription = "First detail paragraph.\n\nSecond detail paragraph remains plain text.";
        model.Gallery = uploaded.Select((path, index) => new ProductGalleryEditRow { Source = path, Alt = $"Gallery sample {index + 1}" }).ToList();
        model.Features = [new() { Text = "Feature A" }, new() { Text = "Feature B" }, new() { Text = "Feature C" }];
        model.RelatedProductIds = related.Select(x => x.Id).ToList();
        await products.UpdateAsync(target.Id, model);

        model.Gallery = [model.Gallery[3], model.Gallery[0], model.Gallery[1], model.Gallery[2]];
        model.Features = [model.Features[2], model.Features[0], model.Features[1]];
        model.RelatedProductIds = [related[2].Id, related[0].Id, related[1].Id];
        await products.UpdateAsync(target.Id, model);
        var detail = (await products.GetDetailAsync(target.Slug))!;
        Check(detail.Gallery.Select(x => x.Source).SequenceEqual(new[] { uploaded[3], uploaded[0], uploaded[1], uploaded[2] }), "Gallery reorder persists exact D/A/B/C order.");
        Check(detail.Features.SequenceEqual(new[] { "Feature C", "Feature A", "Feature B" }), "Feature edit and reorder persist exact order.");
        Check(detail.RelatedProducts.Select(x => x.Slug).SequenceEqual(new[] { related[2].Slug, related[0].Slug, related[1].Slug }), "Related Product selection and reorder persist exact order.");
        Check((await client.GetStringAsync("/shop/" + target.Slug)).Contains(uploaded[3]), "Public Product Detail renders the first reordered gallery image.");

        await using (var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: Origin))
        {
            using var restartedClient = restarted.NewClient();
            var restartedHtml = await restartedClient.GetStringAsync("/shop/" + target.Slug);
            Check(restartedHtml.Contains(uploaded[3]) && restartedHtml.Contains("Feature C") && restartedHtml.IndexOf(related[2].Name, StringComparison.Ordinal) < restartedHtml.IndexOf(related[0].Name, StringComparison.Ordinal),
                "Gallery, features and related order survive refresh and application restart.");
        }

        model.Gallery = [model.Gallery[0]];
        await products.UpdateAsync(target.Id, model);
        var oneHtml = await client.GetStringAsync("/shop/" + target.Slug);
        Check(!oneHtml.Contains("product-gallery__control--previous") && oneHtml.Contains(uploaded[3]), "Single-image gallery hides unnecessary navigation controls.");

        model.Gallery.Clear();
        model.Features.Clear();
        model.RelatedProductIds.Clear();
        await products.UpdateAsync(target.Id, model);
        var emptyHtml = await client.GetStringAsync("/shop/" + target.Slug);
        Check(emptyHtml.Contains(model.ImagePath) && !emptyHtml.Contains("product-gallery__control") && !emptyHtml.Contains("Related products") && !emptyHtml.Contains("What’s Included"),
            "Zero-gallery uses cover fallback while empty Features and Related sections hide cleanly.");
        await using (var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: Origin))
        {
            await using var restartedScope = restarted.Services.CreateAsyncScope();
            var publicProducts = restartedScope.ServiceProvider.GetRequiredService<IProductContentService>();
            var emptyAfterRestart = (await publicProducts.GetDetailAsync(target.Slug))!;
            Check(emptyAfterRestart.Gallery.Count == 0 && emptyAfterRestart.Features.Count == 0 && emptyAfterRestart.RelatedProducts.Count == 0,
                "Intentionally emptied Product detail collections stay empty after restart.");
        }

        model.RelatedProductIds = related.Select(x => x.Id).ToList();
        await products.UpdateAsync(target.Id, model);
        await products.DeleteAsync(related[1].Id);
        detail = (await products.GetDetailAsync(target.Slug))!;
        Check(detail.RelatedProducts.Count == 2 && detail.RelatedProducts.All(x => x.Slug != related[1].Slug), "Deleting a related Product cascades its relation without breaking the detail page.");
        Check(!(await client.GetStringAsync("/sitemap.xml")).Contains("/shop/" + related[1].Slug), "Deleted Product disappears from sitemap.");
        Check((await client.GetAsync("/shop/" + related[1].Slug)).StatusCode == HttpStatusCode.NotFound, "Deleted Product has no stale successful route.");

        await using var db = await factory.CreateDbContextAsync();
        Check(await db.ProductDetailInitializationStates.CountAsync() == 1, "Product detail CRUD never changes the one-time initialization marker.");
    }

    private static async Task RelationValidationAsync(ProductContentService products)
    {
        var product = (await products.ListAsync()).First();
        var model = await products.GetForEditAsync(product.Id);
        model.RelatedProductIds = [product.Id];
        await RejectAsync<ValidationException>(() => products.UpdateAsync(product.Id, model), "Self-related Product rejected.");
        var other = (await products.ListChoicesAsync(product.Id)).First();
        model.RelatedProductIds = [other.Id, other.Id];
        await RejectAsync<ValidationException>(() => products.UpdateAsync(product.Id, model), "Duplicate related Product rejected.");
        model.RelatedProductIds = [other.Id, int.MaxValue];
        await RejectAsync<ValidationException>(() => products.UpdateAsync(product.Id, model), "Missing related Product rejected.");
    }

    private static async Task AuthorizationAsync(IDbContextFactory<ApplicationDbContext> factory, IOptions<IdentityOptions> options, MediaFilePaths paths)
    {
        var anonymous = new TestAuth(new ClaimsPrincipal(new ClaimsIdentity()));
        var media = new LocalMediaStorageService(paths, factory, anonymous, options);
        var service = new ProductContentService(factory, anonymous, options, media, NullLogger<ProductContentService>.Instance);
        await RejectAsync<UnauthorizedAccessException>(() => service.ListChoicesAsync(), "Anonymous Related Product selection read denied.");
        await RejectAsync<UnauthorizedAccessException>(() => service.UpdateAsync(1, new ProductEditModel()), "Anonymous Product detail write denied.");
        Check(await service.GetDetailAsync("productivity-pro") is not null, "Public Product Detail remains anonymous read-only.");
    }

    private static async Task MigrationUpgradeAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), "NexNovaCo.ProductDetail.Tests", Guid.NewGuid().ToString("N"), "upgrade.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=" + path).Options;
        await using var db = new ApplicationDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();
        var detailIndex = Array.FindIndex(migrations, x => x.EndsWith("_AddProductDetails", StringComparison.Ordinal));
        Check(detailIndex == migrations.Length - 1 && detailIndex > 0, "Product Detail uses one latest additive migration.");
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[detailIndex - 1]);
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO Products (DisplayOrder, Name, Slug, Tagline, ShortDescription, Price, OriginalPrice, Badge, ImagePath, UpdatedAtUtc)
            VALUES (1, {"Admin Name"}, {"custom-admin-product"}, {"Admin tagline"}, {"Admin short description"}, {77m}, {88m}, {(int)ProductBadge.Sale}, {ProductImageAssets.Paths[0]}, {new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)});");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO ProductInitializationState (Id) VALUES (1);");
        await migrator.MigrateAsync(migrations[detailIndex]);
        await ShopInitializer.InitializeAsync(db);
        var existing = await db.Products.AsNoTracking().SingleAsync();
        Check(existing.Name == "Admin Name" && existing.Tagline == "Admin tagline" && existing.Price == 77m && existing.Badge == ProductBadge.Sale &&
              existing.UpdatedAtUtc == new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            "Phase 23 upgrade preserves every Admin-edited Phase 22 Product field.");
        Check(existing.FullDescription == existing.ShortDescription && await db.ProductDetailInitializationStates.CountAsync() == 1,
            "Upgrade initializes a safe new description for an Admin-created Product and stores its marker.");
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[detailIndex]], db.Database.ProviderName!);
        Check(migration.UpOperations.All(operation => operation is not DropTableOperation and not DropColumnOperation) &&
              migration.UpOperations.Count(operation => operation is CreateTableOperation) == 4 &&
              migration.UpOperations.Count(operation => operation is AddColumnOperation) == 1,
            "Product Detail migration is additive: one column and four focused tables, with no destructive operations.");
    }

    private static int Count(string text, string value) => text.Split(value, StringSplitOptions.None).Length - 1;
    private static async Task RejectAsync<T>(Func<Task> action, string message) where T : Exception
    {
        try { await action(); }
        catch (T) { Check(true, message); return; }
        Check(false, "Expected " + typeof(T).Name + ": " + message);
    }
    private sealed class TestAuth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
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
