using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public enum MediaKind { Hero, Welcome, Member, Partner, AboutHero, AboutVision, ServicesHero, ServicesBenefits, ProjectsHero, TeamHero, TeamSection, ContactHero, SiteLogo }

public static partial class MediaPolicy
{
    public const string ContactHeroDefault = "image/contact/contact-us.jpg";
    public const string TeamHeroDefault = "image/team/ourteam-header.jpg";
    public const string TeamSectionDefault = "image/team/our-team.jpg";
    public const string ProjectsHeroDefault = "image/home/inner-banner.jpg";
    public const string ServicesHeroDefault = "image/about/2025timeline.jpg";
    public const string ServicesBenefitsDefault = "image/service/Benefit.jpg";
    public const string AboutHeroDefault = "image/home/welcome.jpg";
    public const string AboutVisionDefault = "image/about/vision.png";
    public const string HeroDefault = "image/home/header.jpg";
    public const string WelcomeDefault = "image/home/welcome.jpg";
    public const string MemberFallback = "image/team/our-team.jpg";
    public static int MaxBytes(MediaKind kind) => kind is MediaKind.Hero or MediaKind.Welcome or MediaKind.AboutHero or MediaKind.AboutVision or MediaKind.ServicesHero or MediaKind.ServicesBenefits or MediaKind.ProjectsHero or MediaKind.TeamHero or MediaKind.TeamSection or MediaKind.ContactHero ? 5 * 1024 * 1024 : 3 * 1024 * 1024;
    public static string Folder(MediaKind kind) => kind switch
    {
        MediaKind.Hero or MediaKind.Welcome => "home",
        MediaKind.AboutHero or MediaKind.AboutVision => "about",
        MediaKind.ServicesHero or MediaKind.ServicesBenefits => "services",
        MediaKind.ProjectsHero => "projects",
        MediaKind.TeamHero or MediaKind.TeamSection => "team-page",
        MediaKind.ContactHero => "contact",
        MediaKind.SiteLogo => "site",
        MediaKind.Member => "team",
        MediaKind.Partner => "partners",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public static IReadOnlyList<string> Bundled(MediaKind kind) => kind switch
    {
        MediaKind.Hero => [HeroDefault],
        MediaKind.Welcome => [WelcomeDefault],
        MediaKind.AboutHero => [AboutHeroDefault],
        MediaKind.AboutVision => [AboutVisionDefault],
        MediaKind.ServicesHero => [ServicesHeroDefault],
        MediaKind.ServicesBenefits => [ServicesBenefitsDefault],
        MediaKind.ProjectsHero => [ProjectsHeroDefault],
        MediaKind.ContactHero => [ContactHeroDefault],
        MediaKind.SiteLogo => ["image/logo.png"],
        MediaKind.TeamHero => [TeamHeroDefault],
        MediaKind.TeamSection => [TeamSectionDefault],
        MediaKind.Member => MemberImageAssets.Paths,
        MediaKind.Partner => PartnerLogoAssets.Paths,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public static string Fallback(MediaKind kind) => kind == MediaKind.Member ? MemberFallback : Bundled(kind)[0];
    public static bool IsGenerated(string? path) => path is not null && GeneratedPath().IsMatch(path);
    public static bool IsGenerated(string? path, MediaKind kind) => IsGenerated(path) && path!.StartsWith("uploads/" + Folder(kind) + "/", StringComparison.Ordinal);
    public static bool IsAllowed(string? path, MediaKind kind) => Bundled(kind).Contains(path, StringComparer.Ordinal) || IsGenerated(path, kind);
    public static string ContentType(string path) => Path.GetExtension(path) switch { ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => throw new ValidationException("Unsupported image format.") };
    [GeneratedRegex(@"\Auploads/(?:home|team|partners|about|services|projects|team-page|contact|site)/[a-f0-9]{32}\.(?:jpg|png|webp)\z", RegexOptions.CultureInvariant)]
    private static partial Regex GeneratedPath();
}

public sealed class HomeImagePathAttribute(MediaKind kind) : ValidationAttribute
{
    public override bool IsValid(object? value) => value is string path && MediaPolicy.IsAllowed(path, kind);
    public override string FormatErrorMessage(string name) => "Choose a bundled image or a validated upload.";
}
