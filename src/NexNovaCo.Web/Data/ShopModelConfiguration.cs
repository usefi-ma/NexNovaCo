using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

internal static class ShopModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var product = builder.Entity<ProductEntity>();
        product.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Order", "DisplayOrder > 0");
            table.HasCheckConstraint("CK_Products_Price", "Price >= 0");
            table.HasCheckConstraint("CK_Products_OriginalPrice", "OriginalPrice IS NULL OR OriginalPrice >= 0");
            table.HasCheckConstraint("CK_Products_Badge", "Badge BETWEEN 0 AND 4");
        });
        product.HasKey(x => x.Id);
        product.HasIndex(x => x.DisplayOrder);
        product.HasIndex(x => x.Slug).IsUnique();
        product.Property(x => x.Name).IsRequired().HasMaxLength(80);
        product.Property(x => x.Slug).IsRequired().HasMaxLength(80).UseCollation("NOCASE");
        product.Property(x => x.Tagline).IsRequired().HasMaxLength(120);
        product.Property(x => x.ShortDescription).IsRequired().HasMaxLength(240);
        product.Property(x => x.Price).HasPrecision(10, 2);
        product.Property(x => x.OriginalPrice).HasPrecision(10, 2);
        product.Property(x => x.Badge).HasConversion<int>();
        product.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
        product.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();

        var initialization = builder.Entity<ProductInitializationState>();
        initialization.ToTable("ProductInitializationState", table => table.HasCheckConstraint("CK_ProductInitializationState_Singleton", "Id = 1"));
        initialization.HasKey(x => x.Id);
        initialization.Property(x => x.Id).ValueGeneratedNever();

        var hero = builder.Entity<ShopHeroSettings>();
        hero.ToTable("ShopHeroSettings", table => table.HasCheckConstraint("CK_ShopHeroSettings_Singleton", "Id = 1"));
        hero.HasKey(x => x.Id);
        hero.Property(x => x.Id).ValueGeneratedNever();
        hero.Property(x => x.Eyebrow).IsRequired().HasMaxLength(80);
        hero.Property(x => x.Title).IsRequired().HasMaxLength(120);
        hero.Property(x => x.Description).IsRequired().HasMaxLength(600);
        hero.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        hero.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
        hero.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();

        var section = builder.Entity<ShopProductsSectionSettings>();
        section.ToTable("ShopProductsSectionSettings", table => table.HasCheckConstraint("CK_ShopProductsSectionSettings_Singleton", "Id = 1"));
        section.HasKey(x => x.Id);
        section.Property(x => x.Id).ValueGeneratedNever();
        section.Property(x => x.Eyebrow).IsRequired().HasMaxLength(80);
        section.Property(x => x.Title).IsRequired().HasMaxLength(120);
        section.Property(x => x.Introduction).HasMaxLength(500);
        section.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
    }
}
