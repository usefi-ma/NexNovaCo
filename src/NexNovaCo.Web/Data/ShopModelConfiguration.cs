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
        product.Property(x => x.FullDescription).IsRequired().HasMaxLength(10000);
        product.Property(x => x.Price).HasPrecision(10, 2);
        product.Property(x => x.OriginalPrice).HasPrecision(10, 2);
        product.Property(x => x.Badge).HasConversion<int>();
        product.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
        product.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        product.HasMany(x => x.Gallery).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        product.HasMany(x => x.Features).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        product.HasMany(x => x.RelatedProducts).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        product.HasMany(x => x.RelatedFromProducts).WithOne(x => x.RelatedProduct).HasForeignKey(x => x.RelatedProductId).OnDelete(DeleteBehavior.Cascade);

        var gallery = builder.Entity<ProductGalleryImage>();
        gallery.ToTable("ProductGalleryImages", table => table.HasCheckConstraint("CK_ProductGalleryImages_Order", "DisplayOrder > 0"));
        gallery.HasKey(x => x.Id);
        gallery.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        gallery.Property(x => x.Source).IsRequired().HasMaxLength(200);
        gallery.Property(x => x.Alt).IsRequired().HasMaxLength(200);

        var feature = builder.Entity<ProductFeature>();
        feature.ToTable("ProductFeatures", table => table.HasCheckConstraint("CK_ProductFeatures_Order", "DisplayOrder > 0"));
        feature.HasKey(x => x.Id);
        feature.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        feature.Property(x => x.Text).IsRequired().HasMaxLength(300);

        var related = builder.Entity<ProductRelatedProduct>();
        related.ToTable("ProductRelatedProducts", table =>
        {
            table.HasCheckConstraint("CK_ProductRelatedProducts_Order", "DisplayOrder BETWEEN 1 AND 3");
            table.HasCheckConstraint("CK_ProductRelatedProducts_NoSelf", "ProductId <> RelatedProductId");
        });
        related.HasKey(x => x.Id);
        related.HasIndex(x => new { x.ProductId, x.RelatedProductId }).IsUnique();
        related.HasIndex(x => new { x.ProductId, x.DisplayOrder }).IsUnique();

        var detailInitialization = builder.Entity<ProductDetailInitializationState>();
        detailInitialization.ToTable("ProductDetailInitializationState", table => table.HasCheckConstraint("CK_ProductDetailInitializationState_Singleton", "Id = 1"));
        detailInitialization.HasKey(x => x.Id);
        detailInitialization.Property(x => x.Id).ValueGeneratedNever();

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
