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

internal static class ShopPhaseChecks
{
    public static async Task RunAsync()
    {
        await MigrationAsync();
        await using var app = new AuthFactory(NewPassword(), publicBaseUrl: "https://shop.example.invalid");
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var options = app.Services.GetRequiredService<IOptions<IdentityOptions>>();
        var paths = app.Services.GetRequiredService<MediaFilePaths>();

        await FreshPublicAsync(app, client, factory);

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = (await users.FindByEmailAsync(AuthFactory.Email))!;
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(admin);
        var auth = new TestAuth(principal);
        var media = new LocalMediaStorageService(paths, factory, auth, options);
        var products = new ProductContentService(factory, auth, options, media, NullLogger<ProductContentService>.Instance);
        var shop = new ShopContentService(factory, auth, options, media, products, NullLogger<ShopContentService>.Instance);

        await CrudAndSettingsAsync(app, client, factory, products, shop);
        await MediaAsync(client, media, products);
        await RouteAuthorizationAsync(app, users);
        await AuthorizationAsync(factory, options, paths, products);
        await IntentionalEmptyAsync(app, client, factory, products);
        await NavigationInitializationAsync(app, factory);
    }

    private static async Task FreshPublicAsync(AuthFactory app, HttpClient client, IDbContextFactory<ApplicationDbContext> factory)
    {
        var response = await client.GetAsync("/shop");
        Check(response.StatusCode == HttpStatusCode.OK, "Shop is public.");
        var html = await response.Content.ReadAsStringAsync();
        Check(html.Contains("Digital Tools for a Smarter You") && html.Contains("shop-product-grid"), "Shop renders approved hero and product grid.");
        Check(Count(html, "shop-product-card\"") == ShopDefaults.Products.Count, "Shop renders all seeded Product cards once.");
        Check(html.Contains("shop-product-card__body") && html.Contains("shop-product-card__accent") && html.Contains("View Product"), "Product cards use dedicated angular presentation contract.");
        Check(!html.Contains("shop-product-card__cta\" disabled") && html.Contains("href=\"/shop/productivity-pro\""), "Product Card CTA links to the implemented detail route.");
        Check(html.Contains("https://shop.example.invalid/shop") && html.Contains("shop-hero.png") && html.Contains("css/shop"), "Shop SEO, social image and page stylesheet render.");
        Check((await client.GetStringAsync("/sitemap.xml")).Contains("https://shop.example.invalid/shop/productivity-pro"), "Sitemap includes current Product detail routes.");
        Check(PublicSiteUrls.StaticPaths.Contains("/shop"), "Shop is a static discovery path.");
        foreach (var route in new[] { "/dashboard/content/shop/hero", "/dashboard/content/shop/products", "/dashboard/content/shared-products" })
        {
            var denied = await client.GetAsync(route);
            Check(denied.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, "Anonymous cannot open " + route);
        }
        await using var db = await factory.CreateDbContextAsync();
        Check(await db.Products.CountAsync() == 6 && await db.ProductInitializationStates.CountAsync() == 1, "Fresh database seeds six Products and one persistent marker.");
        Check(await db.ShopHeroSettings.CountAsync() == 1 && await db.ShopProductsSectionSettings.CountAsync() == 1, "Fresh database initializes both Shop settings singletons.");
        Check(!db.Database.HasPendingModelChanges(), "Shop runtime model matches migration snapshot.");
        await using var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: "https://shop.example.invalid");
        using var restartedClient = restarted.NewClient();
        Check(Count(await restartedClient.GetStringAsync("/shop"), "shop-product-card\"") == 6, "Restart does not duplicate Products.");
        await using var restartedScope = restarted.Services.CreateAsyncScope();
        Check(await restartedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Products.CountAsync() == 6, "Restart preserves exactly six seeded rows.");
    }

    private static async Task CrudAndSettingsAsync(AuthFactory app, HttpClient client, IDbContextFactory<ApplicationDbContext> factory,
        ProductContentService products, ShopContentService shop)
    {
        var model = new ProductEditModel
        {
            Name = "Launch Planner", Slug = " Launch-Planner ", Tagline = "Plan the next move",
            ShortDescription = "A focused launch planning workspace.", FullDescription = "A complete launch planning workspace.", Price = 18.50m,
            OriginalPrice = 24m, Badge = ProductBadge.New, ImagePath = ProductImageAssets.Paths[0]
        };
        var id = await products.CreateAsync(model);
        Check((await products.GetForEditAsync(id)).Slug == "launch-planner", "Create normalizes Product slug.");
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Duplicate normalized Product slug rejected.");
        model.Name = "Launch Planner Plus"; model.Badge = ProductBadge.Featured; model.Price = 21m;
        await products.UpdateAsync(id, model);
        Check((await products.GetForEditAsync(id)).Name == model.Name, "Product edit persists canonical fields.");
        var order = (await products.ListAsync()).Select(x => x.Id).Reverse().ToArray();
        await products.ReorderAsync(order);
        Check((await products.ListAsync()).Select(x => x.Id).SequenceEqual(order), "Product reorder persists exact order.");
        await RejectAsync<ValidationException>(() => products.ReorderAsync(order.Skip(1).ToArray()), "Incomplete reorder rejected.");

        var hero = await shop.GetHeroForEditAsync();
        hero.Title = "Useful tools, thoughtfully made";
        await shop.SaveHeroAsync(hero);
        var section = await shop.GetProductsSectionForEditAsync();
        section.Introduction = "A small and practical collection.";
        await shop.SaveProductsSectionAsync(section);
        var publicContent = await shop.ReadPublicAsync();
        Check(publicContent.Hero.Title == hero.Title && publicContent.ProductsSection.Introduction == section.Introduction, "Shop page settings update public content.");
        Check((await client.GetStringAsync("/shop")).Contains(hero.Title), "Shop settings render through public route.");

        await using var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: "https://shop.example.invalid");
        using var restartedClient = restarted.NewClient();
        Check((await restartedClient.GetStringAsync("/shop")).Contains(hero.Title), "Product and page edits persist after restart.");
        await products.DeleteAsync(id);
        Check(!(await products.GetAsync()).Any(x => x.Slug == "launch-planner"), "Product delete removes canonical row.");

        model.Slug = "bad/path";
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Unsafe Product slug rejected.");
        model.Slug = "safe-product"; model.Price = -1m;
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Negative Product price rejected.");
        model.Price = 1.001m;
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Product price precision validated.");
        model.Price = 10m; model.OriginalPrice = 10m;
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Original Product price equal to current price rejected.");
        model.OriginalPrice = 9m;
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Original Product price below current price rejected.");
        model.OriginalPrice = null;
        model.Price = 1m; model.Badge = (ProductBadge)999;
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Unsupported Product badge rejected.");
        model.Badge = ProductBadge.None; model.ImagePath = "../outside.jpg";
        await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Unsafe Product image path rejected.");

        await using var db = await factory.CreateDbContextAsync();
        Check(await db.ProductInitializationStates.CountAsync() == 1, "CRUD never changes Product initialization marker.");
    }

    private static async Task RouteAuthorizationAsync(AuthFactory app, UserManager<ApplicationUser> users)
    {
        var password = NewPassword();
        var viewer = new ApplicationUser { Email = "shop-viewer@example.invalid", UserName = "shop-viewer@example.invalid" };
        Check((await users.CreateAsync(viewer, password)).Succeeded, "Create isolated non-Admin Shop viewer.");
        using var viewerClient = app.NewClient();
        await Login(viewerClient, viewer.Email, password);
        using var adminClient = app.NewClient();
        await Login(adminClient, AuthFactory.Email, app.Password);
        foreach (var route in new[] { "/dashboard/content/shared-products", "/dashboard/content/shared-products/new", "/dashboard/content/shared-products/1", "/dashboard/content/shop/hero", "/dashboard/content/shop/products" })
        {
            var denied = await viewerClient.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Non-Admin Shop route denied: " + route);
            Check((await adminClient.GetAsync(route)).IsSuccessStatusCode, "Admin Shop route allowed: " + route);
        }
    }

    private static async Task MediaAsync(HttpClient client, LocalMediaStorageService media, ProductContentService products)
    {
        var root = Path.GetFullPath("../../../../../src/NexNovaCo.Web/wwwroot", AppContext.BaseDirectory);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, ProductImageAssets.Paths[0]));
        var upload = await media.ReadAsync(new TestFile("product.jpg", "image/jpeg", bytes), MediaKind.Product);
        var path = await media.SaveAsync(upload);
        Check(MediaPolicy.IsGenerated(path, MediaKind.Product) && path.StartsWith("uploads/products/"), "Product upload uses isolated controlled folder.");
        Check(MediaPolicy.MaxBytes(MediaKind.Product) == 3 * 1024 * 1024 && MediaPolicy.MaxBytes(MediaKind.ShopHero) == 5 * 1024 * 1024, "Shop media limits are scoped correctly.");
        var image = await client.GetAsync("/" + path);
        Check(image.StatusCode == HttpStatusCode.OK && image.Content.Headers.ContentType!.MediaType == "image/jpeg" && image.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "Product upload is served as a safe raster.");
        var model = new ProductEditModel { Name = "Uploaded Product", Slug = "uploaded-product", Tagline = "Stored safely", ShortDescription = "Uses a validated uploaded cover.", FullDescription = "A complete description for the uploaded product.", Price = 9m, ImagePath = path };
        var id = await products.CreateAsync(model);
        Check((await products.GetAsync()).Single(x => x.Slug == model.Slug).ImagePath == path, "Product upload persists and reaches public content.");
        await products.DeleteAsync(id);
        foreach (var invalid in new[] { "uploads/products/" + Guid.NewGuid().ToString("N") + ".svg", "uploads/projects/" + Guid.NewGuid().ToString("N") + ".jpg", "uploads/products/" + Guid.NewGuid().ToString("N") + ".jpg" })
        {
            model.Slug = "invalid-" + Guid.NewGuid().ToString("N"); model.ImagePath = invalid;
            await RejectAsync<ValidationException>(() => products.CreateAsync(model), "Cross-kind, missing or unsupported Product media rejected.");
        }
    }

    private static async Task AuthorizationAsync(IDbContextFactory<ApplicationDbContext> factory, IOptions<IdentityOptions> options,
        MediaFilePaths paths, ProductContentService authorized)
    {
        var anonymous = new TestAuth(new ClaimsPrincipal(new ClaimsIdentity()));
        var media = new LocalMediaStorageService(paths, factory, anonymous, options);
        var service = new ProductContentService(factory, anonymous, options, media, NullLogger<ProductContentService>.Instance);
        await RejectAsync<UnauthorizedAccessException>(() => service.ListAsync(), "Anonymous Product service read denied.");
        await RejectAsync<UnauthorizedAccessException>(() => service.CreateAsync(new ProductEditModel()), "Anonymous Product write denied.");
        Check((await authorized.GetAsync()).Count > 0, "Public Product read remains anonymous-safe.");
    }

    private static async Task IntentionalEmptyAsync(AuthFactory app, HttpClient client, IDbContextFactory<ApplicationDbContext> factory, ProductContentService products)
    {
        foreach (var item in (await products.ListAsync()).ToArray()) await products.DeleteAsync(item.Id);
        Check((await products.GetAsync()).Count == 0, "Deleting all Products returns an intentional empty collection.");
        var html = await client.GetStringAsync("/shop");
        Check(html.Contains("Products are coming soon.") && !html.Contains("shop-product-card\""), "Shop renders safe zero-item state.");
        await using (var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: "https://shop.example.invalid"))
        {
            using var restartedClient = restarted.NewClient();
            Check((await restartedClient.GetStringAsync("/shop")).Contains("Products are coming soon."), "Restart keeps intentional empty Product collection.");
            await using var restartedScope = restarted.Services.CreateAsyncScope();
            Check(!await restartedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Products.AnyAsync(), "Restart does not reseed deleted Products.");
        }
        var only = new ProductEditModel { Name = "Only New Product", Slug = "only-new-product", Tagline = "Added after empty", ShortDescription = "An Admin-created item after intentional empty state.", FullDescription = "Admin-created detail content after intentional empty state.", Price = 12m, ImagePath = ProductImageAssets.Paths[0] };
        await products.CreateAsync(only);
        await using var afterAdd = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: "https://shop.example.invalid");
        await using var addScope = afterAdd.Services.CreateAsyncScope();
        var rows = await addScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Products.AsNoTracking().ToListAsync();
        Check(rows.Count == 1 && rows[0].Slug == only.Slug, "Add-after-empty restart retains only the Admin-created Product.");
    }

    private static async Task NavigationInitializationAsync(AuthFactory app, IDbContextFactory<ApplicationDbContext> factory)
    {
        await using (var db = await factory.CreateDbContextAsync())
        {
            Check(await db.NavigationItems.AnyAsync(x => x.Url == "shop"), "Fresh navigation defaults include Shop.");
            var shop = await db.NavigationItems.SingleAsync(x => x.Url == "shop");
            db.NavigationItems.Remove(shop);
            await db.SaveChangesAsync();
        }
        await using var restarted = new AuthFactory(app.Password, databasePath: app.DatabasePath, publicBaseUrl: "https://shop.example.invalid");
        await using var scope = restarted.Services.CreateAsyncScope();
        var db2 = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Check(!await db2.NavigationItems.AnyAsync(x => x.Url == "shop") && await db2.NavigationInitializationStates.CountAsync() == 1, "Existing initialized navigation is never overwritten to add Shop.");
    }

    private static async Task MigrationAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), "NexNovaCo.Shop.Tests", Guid.NewGuid().ToString("N"), "migration.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=" + path).Options;
        await using var db = new ApplicationDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.FindIndex(migrations, x => x.EndsWith("_AddShopFoundation", StringComparison.Ordinal));
        Check(index > 0 && index < migrations.Length - 1 && migrations[^1].EndsWith("_AddProductDetails", StringComparison.Ordinal), "Shop foundation remains followed by the additive Product Detail migration.");
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[index - 1]);
        db.Testimonials.Add(new TestimonialEntity { DisplayOrder = 91, Attribution = "Prior data", Quote = "Preserve this exact row.", UpdatedAtUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
        await migrator.MigrateAsync(migrations[index]);
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[migrations[index]], db.Database.ProviderName!);
        Check(migration.UpOperations.Count == 6 && migration.UpOperations.Count(x => x is CreateTableOperation) == 4 && migration.UpOperations.Count(x => x is CreateIndexOperation) == 2,
            "Shop foundation migration remains limited to four additive tables and two indexes.");
        await migrator.MigrateAsync(migrations[^1]);
        await ShopInitializer.InitializeAsync(db);
        Check(await db.Testimonials.AnyAsync(x => x.Attribution == "Prior data" && x.DisplayOrder == 91), "Shop migration preserves prior CMS rows.");
        Check(await db.Products.CountAsync() == 6 && await db.ProductInitializationStates.CountAsync() == 1, "Shop initializes Products exactly once after upgrade.");
        Check(await db.ProductDetailInitializationStates.CountAsync() == 1, "Upgrade initializes Product detail content once.");
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
