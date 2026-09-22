using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class PartnerEditModel
{
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(200)] public string Description { get; set; } = "";
    [Required, StringLength(200), PartnerLogoPath] public string ImagePath { get; set; } = "";
    public bool HasLogoBackground { get; set; }
    [StringLength(500), PartnerLink] public string? Href { get; set; }

    public Partner ToContent() => new(Name.Trim(), Description.Trim(), ImagePath, HasLogoBackground,
        string.IsNullOrWhiteSpace(Href) ? null : Href);
    public static PartnerEditModel FromContent(Partner content) => new()
    {
        Name = content.Name, Description = content.Description, ImagePath = content.ImagePath,
        HasLogoBackground = content.HasLogoBackground, Href = content.Href
    };
}

public sealed record PartnerListItem(int Id, int DisplayOrder, string Name, string ImagePath, bool HasLogoBackground, string? Href);

// Public asset allow-list, not a second content catalog. No disk access, uploads or remote images.
public static class PartnerLogoAssets
{
    public static IReadOnlyList<string> Paths { get; } = Array.AsReadOnly(new[]
    {
        "image/partnership/TechCo.png", "image/partnership/digitalco.png", "image/partnership/netechco.png",
        "image/partnership/nedigitalco.png", "image/partnership/alphaco.png", "image/partnership/nealphaco.png"
    });
    public static bool IsAllowed(string? path) => Paths.Contains(path, StringComparer.Ordinal);
}

public sealed class PartnerLogoPathAttribute : ValidationAttribute
{
    public PartnerLogoPathAttribute() => ErrorMessage = "Choose one of the existing bundled partner logos.";
    public override bool IsValid(object? value) => value is string path && PartnerLogoAssets.IsAllowed(path);
}

public sealed class PartnerLinkAttribute : ValidationAttribute
{
    public PartnerLinkAttribute() => ErrorMessage = "Use a public site route or an HTTPS URL without credentials, traversal or encoded path separators.";
    public override bool IsValid(object? value)
    {
        if (value is null || value is string { Length: 0 }) return true;
        if (value is not string link || link.Any(char.IsWhiteSpace) || link.Any(char.IsControl) || link.Contains('\\')) return false;
        if (Regex.IsMatch(link, @"^(?:/|/?(?:about|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))$")) return true;
        // Check the original path before Uri normalizes dot segments. Reject encoded path syntax as well.
        if (!link.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrEmpty(uri.Host) || uri.UserInfo.Length != 0) return false;
        var path = link[8..].Split('?', '#')[0];
        return !path.Contains('%') && !path.Split('/').Any(segment => segment is "." or "..");
    }
}
