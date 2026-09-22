using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Hero, Welcome, Services, Projects and Team intros are CMS-backed. Other Home content retains its approved static sources.
/// Editable content is read on every request/navigation, never held in the static snapshot.
/// </summary>
public sealed class HomeContentService(IProjectCatalog projectCatalog, IMemberCatalog memberCatalog,
    IHomeHeroContentService heroContent, IHomeWelcomeContentService welcomeContent,
    IHomeServicesSectionContentService servicesContent, IHomeProjectsSectionContentService projectsContent,
    IHomeTeamSectionContentService teamContent) : IHomeContentService
{
    private readonly Lazy<Task<HomeContent>> _content = new(() => LoadAsync(projectCatalog, memberCatalog));

    public async Task<HomeContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var content = await _content.Value.WaitAsync(cancellationToken);
        return content with
        {
            Hero = await heroContent.GetAsync(cancellationToken),
            Welcome = await welcomeContent.GetAsync(cancellationToken),
            ServicesHeading = await servicesContent.GetAsync(cancellationToken),
            ProjectsHeading = await projectsContent.GetAsync(cancellationToken),
            Team = await teamContent.GetAsync(cancellationToken)
        };
    }

    private static async Task<HomeContent> LoadAsync(IProjectCatalog projectCatalog, IMemberCatalog memberCatalog)
    {
        var projects = await projectCatalog.GetAsync();
        var members = await memberCatalog.GetAsync();
        // Home keeps its original five featured identities from the same catalog as the listing.
        var featuredProjects = new[] { "nexconnect", "payflowx", "medilink", "tradesync", "eduvance" }
            .Select(id => projects.Single(project => project.Slug == id)).ToArray();
        var featuredMembers = new[] { "emilyjohnson", "emmawilliams", "sophialee", "danielkim" }
            .Select(id => members.Single(member => member.Slug == id)).ToArray();

        return new HomeContent(
            HomeHeroDefaults.Content,
            HomeWelcomeDefaults.Content,
            HomeServicesSectionDefaults.Content,
            ServiceCatalog.HomeFeatured,
            HomeProjectsSectionDefaults.Content,
            featuredProjects,
            HomeTeamSectionDefaults.Content,
            featuredMembers,
            [new("PROJECTS", 450), new("CLIENTS", 3000), new("EMPLOYEES", 1000), new("AWARDS", 26)],
            PartnerCatalog.Heading, PartnerCatalog.All,
            TestimonialCatalog.All, TestimonialCatalog.Brand);
    }

}
