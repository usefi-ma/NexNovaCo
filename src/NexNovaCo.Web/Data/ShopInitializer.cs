using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class ShopInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        if (!await database.ShopHeroSettings.AnyAsync(cancellationToken))
        {
            var hero = new ShopHeroSettings();
            hero.SetContent(ShopHeroEditModel.Approved());
            database.ShopHeroSettings.Add(hero);
        }
        if (!await database.ShopProductsSectionSettings.AnyAsync(cancellationToken))
        {
            var section = new ShopProductsSectionSettings();
            section.SetContent(ShopProductsSectionEditModel.Approved());
            database.ShopProductsSectionSettings.Add(section);
        }

        if (!await database.ProductInitializationStates.AnyAsync(cancellationToken))
        {
            if (!await database.Products.AnyAsync(cancellationToken))
            {
                var order = 0;
                foreach (var content in ShopDefaults.Products)
                {
                    var product = new ProductEntity { DisplayOrder = ++order };
                    product.SetContent(new ProductEditModel
                    {
                        Name = content.Name, Slug = content.Slug, Tagline = content.Tagline,
                        ShortDescription = content.ShortDescription, Price = content.Price,
                        OriginalPrice = content.OriginalPrice, Badge = content.Badge, ImagePath = content.ImagePath
                    });
                    database.Products.Add(product);
                }
            }
            database.ProductInitializationStates.Add(new ProductInitializationState());
        }

        // Product ids are needed by the ordered self-relations below.
        await database.SaveChangesAsync(cancellationToken);

        if (!await database.ProductDetailInitializationStates.AnyAsync(cancellationToken))
        {
            var products = await database.Products
                .Include(x => x.Gallery)
                .Include(x => x.Features)
                .Include(x => x.RelatedProducts)
                .AsSingleQuery()
                .ToListAsync(cancellationToken);
            var bySlug = products.ToDictionary(x => x.Slug, StringComparer.Ordinal);
            foreach (var product in products)
            {
                var hasApprovedDetail = ProductDetailDefaults.BySlug.TryGetValue(product.Slug, out var detail);
                if (string.IsNullOrWhiteSpace(product.FullDescription))
                    product.FullDescription = hasApprovedDetail ? detail!.FullDescription : product.ShortDescription;
                if (!hasApprovedDetail) continue;
                var approved = detail!;
                if (product.Gallery.Count == 0)
                    product.Gallery.AddRange(approved.Gallery.Select((image, index) => new ProductGalleryImage
                    {
                        Source = image.Source,
                        Alt = image.Alt,
                        DisplayOrder = index + 1
                    }));
                if (product.Features.Count == 0)
                    product.Features.AddRange(approved.Features.Select((text, index) => new ProductFeature
                    {
                        Text = text,
                        DisplayOrder = index + 1
                    }));
                if (product.RelatedProducts.Count == 0)
                    product.RelatedProducts.AddRange(approved.RelatedSlugs
                        .Where(bySlug.ContainsKey)
                        .Select((slug, index) => new ProductRelatedProduct
                        {
                            RelatedProductId = bySlug[slug].Id,
                            DisplayOrder = index + 1
                        }));
            }
            database.ProductDetailInitializationStates.Add(new ProductDetailInitializationState());
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
