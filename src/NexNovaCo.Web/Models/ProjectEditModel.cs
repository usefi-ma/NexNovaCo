using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ProjectEditModel : IValidatableObject
{
    [Required, StringLength(80)] public string Slug { get; set; } = "";
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(120)] public string Tagline { get; set; } = "";
    [Required, StringLength(300)] public string Description { get; set; } = "";
    [Required, StringLength(10000)] public string FullDescription { get; set; } = "";
    [Required, ProjectImagePath] public string ImagePath { get; set; } = "";
    [StringLength(160)] public string? Client { get; set; }
    [StringLength(160)] public string? Category { get; set; }
    [StringLength(80)] public string? Date { get; set; }
    // The approved source has free-text metadata, not a technology collection.
    [StringLength(500)] public string? Technologies { get; set; }
    public List<ProjectGalleryEditRow> Gallery { get; set; } = [];
    public List<ProjectFeatureEditRow> Features { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!ProjectSlugs.IsValid(ProjectSlugs.Normalize(Slug)))
            yield return new("Use lowercase letters, numbers and single hyphens for the slug.", [nameof(Slug)]);
        if (Gallery is null || Features is null)
        {
            yield return new("Gallery and Features must be valid collections.");
            yield break;
        }
        foreach (var row in Gallery)
            if (row is null || !ProjectImageAssets.IsAllowed(row.Source) || string.IsNullOrWhiteSpace(row.Alt) || row.Alt.Length > 200)
                yield return new("Each gallery row needs an approved image and alt text (up to 200 characters).", [nameof(Gallery)]);
        foreach (var row in Features)
            if (row is null || string.IsNullOrWhiteSpace(row.Text) || row.Text.Length > 300)
                yield return new("Each feature needs text up to 300 characters.", [nameof(Features)]);
    }
    public static ProjectEditModel FromContent(ProjectDetail content) => new()
    {
        Slug = content.Summary.Slug, Name = content.Summary.Name, Tagline = content.Summary.Tagline,
        Description = content.Summary.Description, ImagePath = content.Summary.ImagePath,
        FullDescription = content.FullDescription, Client = content.Metadata.Client, Category = content.Metadata.Category,
        Date = content.Metadata.Date, Technologies = content.Metadata.Technologies,
        Gallery = content.Gallery.Select(x => new ProjectGalleryEditRow { Source = x.Source, Alt = x.Alt }).ToList(),
        Features = content.Features.Select(x => new ProjectFeatureEditRow { Text = x }).ToList()
    };
}
public sealed class ProjectGalleryEditRow
{
    public string Source { get; set; } = "";
    public string Alt { get; set; } = "";
}
public sealed class ProjectFeatureEditRow { public string Text { get; set; } = ""; }
public sealed record ProjectListItem(int Id, int DisplayOrder, string Name, string Slug, string ImagePath, int? HomeDisplayOrder);
public static partial class ProjectSlugs
{
    public static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
    public static bool IsValid(string value) => value.Length is > 0 and <= 80 && SlugPattern().IsMatch(value);
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
public static class ProjectImageAssets
{
    public static IReadOnlyList<string> Paths { get; } = Array.AsReadOnly(new[]
    {
        "image/project/nexconnect.jpg", "image/project/payflowx.jpg", "image/project/medilink.jpg",
        "image/project/tradesync.jpg", "image/project/eduvance.jpg", "image/project/autotracker.jpg",
        "image/project/finvault.jpg", "image/project/nextConnectProject.jpg", "image/project/nextConnectInnerProject.jpg"
    });
    public static bool IsAllowed(string? path) => Paths.Contains(path, StringComparer.Ordinal);
}
public sealed class ProjectImagePathAttribute : ValidationAttribute
{
    public ProjectImagePathAttribute() => ErrorMessage = "Choose an approved bundled project image.";
    public override bool IsValid(object? value) => value is string path && ProjectImageAssets.IsAllowed(path);
}
