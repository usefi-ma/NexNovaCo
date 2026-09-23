using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class MemberEditModel : IValidatableObject
{
    [Required, StringLength(80)] public string Slug { get; set; } = "";
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(100)] public string Role { get; set; } = "";
    [Required, StringLength(300)] public string Introduction { get; set; } = "";
    [Required, StringLength(10000)] public string Biography { get; set; } = "";
    [Required, MemberImagePath] public string ImagePath { get; set; } = "";
    [StringLength(254), MemberEmail] public string? Email { get; set; }
    // Fixed platforms mirror the current Summary/SocialLinks contract; blanks remove destinations.
    [StringLength(500), MemberHttpsUrl] public string? LinkedIn { get; set; }
    [StringLength(500), MemberHttpsUrl] public string? Telegram { get; set; }
    public List<MemberSkillEditRow> Skills { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!MemberSlugs.IsValid(MemberSlugs.Normalize(Slug)))
            yield return new("Use letters, numbers and single hyphens for the slug.", [nameof(Slug)]);
        if (Skills is null)
        {
            yield return new("Skills must be a valid collection.");
            yield break;
        }
        foreach (var row in Skills)
            if (row is null || string.IsNullOrWhiteSpace(row.Text) || row.Text.Length > 200)
                yield return new("Each skill needs text up to 200 characters.", [nameof(Skills)]);
    }
    public static MemberEditModel FromContent(MemberDetail content) => new()
    {
        Slug = content.Summary.Slug, Name = content.Summary.Name, Role = content.Summary.Role,
        Introduction = content.Summary.Introduction, Biography = content.Biography, ImagePath = content.Summary.ImagePath,
        Email = content.Summary.Email, LinkedIn = content.Summary.LinkedIn, Telegram = content.Summary.Telegram,
        Skills = content.Skills.Select(x => new MemberSkillEditRow { Text = x }).ToList()
    };
}
public sealed class MemberSkillEditRow { public string Text { get; set; } = ""; }
public sealed record MemberListItem(int Id, int DisplayOrder, string Name, string Slug, string Role, string ImagePath, int? HomeDisplayOrder);
public static partial class MemberSlugs
{
    public static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
    public static bool IsValid(string value) => value.Length is > 0 and <= 80 && Pattern().IsMatch(value);
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
public static class MemberImageAssets
{
    public static IReadOnlyList<string> Paths { get; } = Array.AsReadOnly(new[]
    {
        "image/team/emily-johnson.jpg", "image/team/emma-williams.jpg", "image/team/sophia-lee.jpg",
        "image/team/daniel-kim.jpg", "image/team/lena-alvarez.jpg", "image/team/james-park.jpg"
    });
    public static bool IsAllowed(string? path) => Paths.Contains(path, StringComparer.Ordinal);
}
public sealed class MemberImagePathAttribute : ValidationAttribute
{
    public MemberImagePathAttribute() => ErrorMessage = "Choose an approved bundled Team portrait.";
    public override bool IsValid(object? value) => value is string path && MemberImageAssets.IsAllowed(path);
}
public sealed class MemberHttpsUrlAttribute : ValidationAttribute
{
    public MemberHttpsUrlAttribute() => ErrorMessage = "Enter a full HTTPS URL without credentials, whitespace or unsafe characters, or leave blank.";
    public override bool IsValid(object? value)
    {
        if (value is null or "") return true;
        if (value is not string text || text.Any(char.IsWhiteSpace) || text.Any(char.IsControl) || text.IndexOfAny(['\\', '<', '>', '"']) >= 0) return false;
        return text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);
    }
}
public sealed class MemberEmailAttribute : ValidationAttribute
{
    public MemberEmailAttribute() => ErrorMessage = "Enter a single email address without a mailto prefix or query, or leave blank.";
    public override bool IsValid(object? value)
    {
        if (value is null or "") return true;
        return value is string text && !text.Any(char.IsWhiteSpace) && !text.Any(char.IsControl) &&
            text.IndexOfAny(['?', '#', '&', '%', ':', '/', '\\', '<', '>', '"', ',', ';']) < 0 &&
            new EmailAddressAttribute().IsValid(text);
    }
}
