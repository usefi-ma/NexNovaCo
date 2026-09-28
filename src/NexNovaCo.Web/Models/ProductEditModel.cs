using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ProductEditModel : IValidatableObject
{
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string Slug { get; set; } = "";
    [Required, StringLength(120)] public string Tagline { get; set; } = "";
    [Required, StringLength(240)] public string ShortDescription { get; set; } = "";
    [Range(typeof(decimal), "0", "999999.99")] public decimal Price { get; set; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal? OriginalPrice { get; set; }
    public ProductBadge Badge { get; set; }
    [Required, ProductImagePath] public string ImagePath { get; set; } = "";

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
    }
}

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
        "image/project/tradesync.jpg", "image/project/eduvance.jpg", "image/project/finvault.jpg"
    });
}

public sealed class ProductImagePathAttribute : ValidationAttribute
{
    public ProductImagePathAttribute() => ErrorMessage = "Choose an approved product image or validated upload.";
    public override bool IsValid(object? value) => value is string path && MediaPolicy.IsAllowed(path, MediaKind.Product);
}
