using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class ShopHeroEditModel
{
    [Required, StringLength(80)] public string Eyebrow { get; set; } = "";
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [Required, StringLength(600)] public string Description { get; set; } = "";
    [Required, StringLength(60)] public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.ShopHero)] public string ImagePath { get; set; } = "";
    public static ShopHeroEditModel Approved() => new()
    {
        Eyebrow = ShopDefaults.Hero.Eyebrow,
        Title = ShopDefaults.Hero.Title,
        Description = ShopDefaults.Hero.Description,
        CtaLabel = ShopDefaults.Hero.CtaLabel,
        ImagePath = ShopDefaults.Hero.ImagePath
    };
}

public sealed class ShopProductsSectionEditModel
{
    [Required, StringLength(80)] public string Eyebrow { get; set; } = "";
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [StringLength(500)] public string? Introduction { get; set; }
    public static ShopProductsSectionEditModel Approved() => new()
    {
        Eyebrow = ShopDefaults.ProductsSection.Eyebrow,
        Title = ShopDefaults.ProductsSection.Title,
        Introduction = ShopDefaults.ProductsSection.Introduction
    };
}

public static class ShopDefaults
{
    public static ShopHeroContent Hero { get; } = new(
        "Tech for a brighter tomorrow",
        "Shop",
        "Curated digital tools, templates, and resources to help you work smarter, faster, and achieve more.",
        "Browse Products",
        MediaPolicy.ShopHeroDefault);

    public static ShopProductsSectionContent ProductsSection { get; } = new(
        "Our Products",
        "Digital Tools for a Smarter You",
        "Practical resources created to help ideas move from plan to progress.");

    public static IReadOnlyList<ProductSummary> Products { get; } =
    [
        new("productivity-pro", "Productivity Pro", "Organize Your Best Work", "A focused planning template to organize your work and life.", 29m, 39m, ProductBadge.BestSeller, ProductImageAssets.Paths[0]),
        new("social-media-content-kit", "Social Media Content Kit", "Plan. Publish. Grow.", "Ready-to-use templates for consistent, engaging content.", 25m, null, ProductBadge.New, ProductImageAssets.Paths[1]),
        new("pitch-deck-template", "Pitch Deck Template", "Ideas Into Impact", "A modern, investor-ready presentation framework.", 35m, null, ProductBadge.Featured, ProductImageAssets.Paths[2]),
        new("freelancer-business-kit", "Freelancer Business Kit", "Run Your Business Better", "Contracts, invoices, and practical templates for independent work.", 29m, 39m, ProductBadge.None, ProductImageAssets.Paths[3]),
        new("website-ui-kit", "Website UI Kit", "Build Modern Interfaces", "A flexible collection of modern components for your next project.", 39m, 49m, ProductBadge.Featured, ProductImageAssets.Paths[4]),
        new("email-marketing-kit", "Email Marketing Kit", "Connect With Your Audience", "Campaign templates and strategies designed to grow your audience.", 27m, null, ProductBadge.Sale, ProductImageAssets.Paths[5])
    ];
}
