namespace NexNovaCo.Web.Models;

public sealed record MemberDetail(TeamMemberSummary Summary, string Biography, IReadOnlyList<string> Skills);
public sealed record TeamContent(InnerPageHeroContent Hero, string Eyebrow, TeamSectionContent Introduction,
    IReadOnlyList<TeamMemberSummary> Members, string HeroImagePath = MediaPolicy.TeamHeroDefault,
    string SectionImagePath = MediaPolicy.TeamSectionDefault);

// The approved Home and Team cards use different portrait silhouettes, not arbitrary styles.
public enum TeamMemberCardContext { Featured, Listing }
