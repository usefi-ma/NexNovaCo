namespace NexNovaCo.Web.Models;

// Only the content contracts used by Home and its reusable cards. No persistence concerns.
public sealed record HomeHeroContent(string OpeningLine, string EmphasisLine, string ClosingLine,
    string Description, string CtaLabel, string CtaHref);
public sealed record SectionHeading(string Title, string? Description = null);
public sealed record WelcomeContent(string Title, string Introduction, IReadOnlyList<string> Paragraphs,
    string CtaLabel, string CtaHref);
public sealed record ServiceSummary(string Id, string Name, string Tagline, string Description, string IconPath);
public sealed record ProjectSummary(string Slug, string Name, string Tagline, string Description, string ImagePath)
{
    public string DetailHref => $"projects/{Slug}";
}
public sealed record TeamMemberSummary(string Slug, string Name, string Role, string Introduction,
    string ImagePath, string? Email, string? LinkedIn, string? Telegram)
{
    public string ProfileHref => $"team/{Slug}";
}
public sealed record TeamSectionContent(string Title, string Introduction, string Highlight,
    string Description, string CtaLabel, string CtaHref);
public sealed record Statistic(string Label, int Value);
public sealed record Partner(string Name, string Description, string ImagePath,
    bool HasLogoBackground = false, string? Href = null);
public sealed record Testimonial(string Attribution, IReadOnlyList<string> Paragraphs);
public sealed record HomeContent(HomeHeroContent Hero, WelcomeContent Welcome,
    SectionHeading ServicesHeading, IReadOnlyList<ServiceSummary> Services,
    SectionHeading ProjectsHeading, IReadOnlyList<ProjectSummary> Projects,
    TeamSectionContent Team, IReadOnlyList<TeamMemberSummary> Members,
    IReadOnlyList<Statistic> Statistics, SectionHeading PartnersHeading, IReadOnlyList<Partner> Partners,
    IReadOnlyList<Testimonial> Testimonials, SectionHeading TestimonialBrand);
