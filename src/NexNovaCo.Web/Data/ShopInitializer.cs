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

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
