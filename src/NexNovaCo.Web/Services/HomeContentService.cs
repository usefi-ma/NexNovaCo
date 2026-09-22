using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Home intros and Statistics are CMS-backed. Shared entity collections retain their canonical sources.
/// Editable content is read on every request/navigation, never held in the static snapshot.
/// </summary>
public sealed class HomeContentService(IProjectCatalog projectCatalog, IMemberCatalog memberCatalog,
    IHomeHeroContentService heroContent, IHomeWelcomeContentService welcomeContent,
    IHomeServicesSectionContentService servicesContent, IHomeProjectsSectionContentService projectsContent,
    IHomeTeamSectionContentService teamContent, IHomeStatisticsContentService statisticsContent,
    IHomePartnersSectionContentService partnersContent,
    IHomeTestimonialsSectionContentService testimonialsContent,
    ITestimonialContentService testimonials, IPartnerContentService partners) : IHomeContentService
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
            Team = await teamContent.GetAsync(cancellationToken),
            Statistics = await statisticsContent.GetAsync(cancellationToken),
            Partners = await partners.GetAsync(cancellationToken),
            PartnersHeading = await partnersContent.GetAsync(cancellationToken),
            TestimonialBrand = await testimonialsContent.GetAsync(cancellationToken),
            Testimonials = await testimonials.GetAsync(cancellationToken)
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
            HomeStatisticsDefaults.Content,
            HomePartnersSectionDefaults.Content, [],
            [], HomeTestimonialsSectionDefaults.Content);
    }

}
