namespace NexNovaCo.Web.Models;

public sealed record AboutStoryContent(string Title, string Subtitle, IReadOnlyList<string> Introduction,
    IReadOnlyList<string> Detail, string Closing);
public sealed record AboutVisionContent(string Title, IReadOnlyList<string> Paragraphs,
    string CtaLabel, string CtaHref, string ImagePath, string ImageAlt);
public sealed record TimelineMilestone(int Year, string Description);
public sealed record AboutTimelineContent(SectionHeading Heading, IReadOnlyList<TimelineMilestone> Milestones,
    string CtaLabel, string CtaHref);
public sealed record AboutMissionContent(SectionHeading Brand, string Title,
    IReadOnlyList<string> Paragraphs, IReadOnlyList<string> Commitments);
public sealed record AboutContent(InnerPageHeroContent Hero, AboutStoryContent Story,
    AboutVisionContent Vision, AboutTimelineContent Timeline, AboutMissionContent Mission,
    SectionHeading PartnersHeading, IReadOnlyList<Partner> Partners, string HeroImagePath = MediaPolicy.AboutHeroDefault);
