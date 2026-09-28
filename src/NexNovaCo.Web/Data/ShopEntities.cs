using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ProductEntity
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public ProductBadge Badge { get; set; }
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }

    public ProductSummary ToContent() => new(Slug, Name, Tagline, ShortDescription, Price, OriginalPrice, Badge, ImagePath);
    public ProductEditModel ToEditModel() => new()
    {
        Name = Name, Slug = Slug, Tagline = Tagline, ShortDescription = ShortDescription,
        Price = Price, OriginalPrice = OriginalPrice, Badge = Badge, ImagePath = ImagePath
    };
    public void SetContent(ProductEditModel model)
    {
        Name = model.Name.Trim();
        Slug = ProductSlugs.Normalize(model.Slug);
        Tagline = model.Tagline.Trim();
        ShortDescription = model.ShortDescription.Trim();
        Price = model.Price;
        OriginalPrice = model.OriginalPrice;
        Badge = model.Badge;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ProductInitializationState { public int Id { get; set; } = 1; }

public sealed class ShopHeroSettings
{
    public int Id { get; set; } = 1;
    public string Eyebrow { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ShopHeroEditModel ToEditModel() => new() { Eyebrow = Eyebrow, Title = Title, Description = Description, CtaLabel = CtaLabel, ImagePath = ImagePath };
    public void SetContent(ShopHeroEditModel model)
    {
        Eyebrow = model.Eyebrow.Trim(); Title = model.Title.Trim(); Description = model.Description.Trim();
        CtaLabel = model.CtaLabel.Trim(); ImagePath = model.ImagePath; UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ShopProductsSectionSettings
{
    public int Id { get; set; } = 1;
    public string Eyebrow { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Introduction { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ShopProductsSectionEditModel ToEditModel() => new() { Eyebrow = Eyebrow, Title = Title, Introduction = Introduction };
    public void SetContent(ShopProductsSectionEditModel model)
    {
        Eyebrow = model.Eyebrow.Trim(); Title = model.Title.Trim(); Introduction = string.IsNullOrWhiteSpace(model.Introduction) ? null : model.Introduction.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
