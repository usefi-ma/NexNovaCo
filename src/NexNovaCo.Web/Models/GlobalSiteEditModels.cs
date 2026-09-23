using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public enum SocialPlatform { Email, LinkedIn, Telegram }

public sealed class SiteIdentityEditModel
{
    [Required, StringLength(80)]
    public string SiteName { get; set; } = "NexNovaCo";
    [Required, StringLength(200), HomeImagePath(MediaKind.SiteLogo)]
    public string ImagePath { get; set; } = "image/logo.png";
    public static SiteIdentityEditModel Approved() => new();
}

public sealed class FooterEditModel
{
    [Required, StringLength(1000)]
    public string Description { get; set; } = "NexNovaCo specializes in delivering innovative solutions and exceptional services tailored to your needs. Empowering businesses with creativity and expertise.";
    [Required, StringLength(200)]
    public string Copyright { get; set; } = "© 2026. All rights reserved.";
    [Required, StringLength(120)]
    public string NewsletterHeading { get; set; } = "Subscribe to our newsletter";
    [Required, StringLength(160)]
    public string NewsletterPlaceholder { get; set; } = "Your Email";
    [Required, StringLength(60)]
    public string NewsletterSubmitLabel { get; set; } = "Submit";
    public bool NewsletterVisible { get; set; } = true;
    public static FooterEditModel Approved() => new();
}

public sealed class NavigationEditModel
{
    [Required, StringLength(40)]
    public string Label { get; set; } = "";
    [Required, StringLength(200), PublicNavigationRoute]
    public string Url { get; set; } = "/";
}

public sealed class SocialLinkEditModel : IValidatableObject
{
    [EnumDataType(typeof(SocialPlatform))]
    public SocialPlatform Platform { get; set; } = SocialPlatform.LinkedIn;
    [StringLength(500), GlobalSocialUrl]
    public string Url { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Platform == SocialPlatform.Email && !string.IsNullOrEmpty(Url))
            yield return new("Email uses Global Settings → Contact Info; do not duplicate its address here.", [nameof(Url)]);
    }
}

public sealed class SiteContactEditModel
{
    [Required, StringLength(80)]
    public string Phone { get; set; } = "";
    [Required, StringLength(254), EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(500)]
    public string Address { get; set; } = "";
}

public sealed class PublicNavigationRouteAttribute : ValidationAttribute
{
    public PublicNavigationRouteAttribute() => ErrorMessage = "Use /, /about, /services, /projects, /team, /contact, or a project/member detail route.";
    public override bool IsValid(object? value) => value is string route && Regex.IsMatch(route,
        @"\A(?:/|/?(?:about|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))\z", RegexOptions.CultureInvariant);
}

public sealed class GlobalSocialUrlAttribute : ValidationAttribute
{
    public GlobalSocialUrlAttribute() => ErrorMessage = "Use an HTTPS URL without credentials, or leave empty for the existing decorative icon.";
    public override bool IsValid(object? value) => value is null or "" || value is string url &&
        !url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '<' or '>' or '"' or '\\') &&
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.HostNameType == UriHostNameType.Dns && uri.UserInfo.Length == 0 && uri.IsDefaultPort;
}

public sealed record NavigationListItem(int Id, int DisplayOrder, string Label, string Url);
public sealed record SocialLinkListItem(int Id, int DisplayOrder, SocialPlatform Platform, string? Url);

public static class GlobalSiteDefaults
{
    public static IReadOnlyList<NavigationListItem> Navigation =>
    [
        new(1, 1, "Home", "/"), new(2, 2, "About", "about"), new(3, 3, "Services", "services"),
        new(4, 4, "Projects", "projects"), new(5, 5, "Team", "team"), new(6, 6, "Contact", "contact")
    ];
    // Preserve the existing email link and two decorative icons without inventing social URLs.
    public static IReadOnlyList<SocialLinkListItem> SocialLinks =>
    [new(1, 1, SocialPlatform.Email, ""), new(2, 2, SocialPlatform.LinkedIn, ""), new(3, 3, SocialPlatform.Telegram, "")];
}
