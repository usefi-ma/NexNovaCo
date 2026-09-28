using System.Globalization;

namespace NexNovaCo.Web.Models;

public enum ProductBadge { None, BestSeller, New, Featured, Sale }

public sealed record ProductSummary(
    string Slug,
    string Name,
    string Tagline,
    string ShortDescription,
    decimal Price,
    decimal? OriginalPrice,
    ProductBadge Badge,
    string ImagePath);

// Detail media is deliberately separate from the cover used by cards and social previews.
public sealed record ProductImage(string Source, string Alt);
public sealed record ProductDetail(
    ProductSummary Summary,
    string FullDescription,
    IReadOnlyList<ProductImage> Gallery,
    IReadOnlyList<string> Features,
    IReadOnlyList<ProductSummary> RelatedProducts);

public sealed record ShopHeroContent(string Eyebrow, string Title, string Description, string CtaLabel, string ImagePath);
public sealed record ShopProductsSectionContent(string Eyebrow, string Title, string? Introduction);
public sealed record ShopContent(ShopHeroContent Hero, ShopProductsSectionContent ProductsSection, IReadOnlyList<ProductSummary> Products);

public static class ProductPresentation
{
    public static string Price(decimal value) => value.ToString("$0.##", CultureInfo.GetCultureInfo("en-US"));
    public static string? Badge(ProductBadge value) => value switch
    {
        ProductBadge.BestSeller => "Best Seller",
        ProductBadge.New => "New",
        ProductBadge.Featured => "Featured",
        ProductBadge.Sale => "Sale",
        _ => null
    };
}
