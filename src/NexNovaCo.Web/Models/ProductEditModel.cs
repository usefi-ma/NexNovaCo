using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ProductEditModel : IValidatableObject
{
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string Slug { get; set; } = "";
    [Required, StringLength(120)] public string Tagline { get; set; } = "";
    [Required, StringLength(240)] public string ShortDescription { get; set; } = "";
    [Required, StringLength(10000)] public string FullDescription { get; set; } = "";
    [Range(typeof(decimal), "0", "999999.99")] public decimal Price { get; set; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal? OriginalPrice { get; set; }
    public ProductBadge Badge { get; set; }
    [Required, ProductImagePath] public string ImagePath { get; set; } = "";
    public List<ProductGalleryEditRow> Gallery { get; set; } = [];
    public List<ProductFeatureEditRow> Features { get; set; } = [];
    public List<int> RelatedProductIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ProductSlugs.IsValid(ProductSlugs.Normalize(Slug)))
            yield return new("Use lowercase letters, numbers and single hyphens for the slug.", [nameof(Slug)]);
        if (!Enum.IsDefined(Badge))
            yield return new("Choose a supported product badge.", [nameof(Badge)]);
        if (Decimal.Round(Price, 2) != Price)
            yield return new("Price supports up to two decimal places.", [nameof(Price)]);
        if (OriginalPrice is decimal original && Decimal.Round(original, 2) != original)
            yield return new("Original price supports up to two decimal places.", [nameof(OriginalPrice)]);
        if (Gallery is null || Features is null || RelatedProductIds is null)
        {
            yield return new("Gallery, Features and Related Products must be valid collections.");
            yield break;
        }
        foreach (var row in Gallery)
            if (row is null || !MediaPolicy.IsAllowed(row.Source, MediaKind.Product) || string.IsNullOrWhiteSpace(row.Alt) || row.Alt.Length > 200)
                yield return new("Each gallery image needs an approved image and meaningful alt text up to 200 characters.", [nameof(Gallery)]);
        foreach (var row in Features)
            if (row is null || string.IsNullOrWhiteSpace(row.Text) || row.Text.Length > 300)
                yield return new("Each feature needs text up to 300 characters.", [nameof(Features)]);
        if (RelatedProductIds.Count > 3)
            yield return new("Choose no more than three related products.", [nameof(RelatedProductIds)]);
        if (RelatedProductIds.Any(id => id <= 0) || RelatedProductIds.Distinct().Count() != RelatedProductIds.Count)
            yield return new("Related products must be unique valid products.", [nameof(RelatedProductIds)]);
    }
}

public sealed class ProductGalleryEditRow
{
    public string Source { get; set; } = "";
    public string Alt { get; set; } = "";
}

public sealed class ProductFeatureEditRow { public string Text { get; set; } = ""; }

public sealed record ProductSelectionItem(int Id, string Name, string Slug);

public sealed record ProductListItem(int Id, int DisplayOrder, string Name, string Slug, decimal Price, ProductBadge Badge, string ImagePath, DateTime UpdatedAtUtc);

public static partial class ProductSlugs
{
    public static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
    public static bool IsValid(string value) => value.Length is > 0 and <= 80 && SlugPattern().IsMatch(value);
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}

public static class ProductImageAssets
{
    public static IReadOnlyList<string> Paths { get; } = Array.AsReadOnly(new[]
    {
        "image/project/nexconnect.jpg", "image/project/payflowx.jpg", "image/project/medilink.jpg",
        "image/project/tradesync.jpg", "image/project/eduvance.jpg", "image/project/finvault.jpg",
        "image/project/autotracker.jpg", "image/project/nextConnectProject.jpg", "image/project/nextConnectInnerProject.jpg"
    });
}

public sealed class ProductImagePathAttribute : ValidationAttribute
{
    public ProductImagePathAttribute() => ErrorMessage = "Choose an approved product image or validated upload.";
    public override bool IsValid(object? value) => value is string path && MediaPolicy.IsAllowed(path, MediaKind.Product);
}
