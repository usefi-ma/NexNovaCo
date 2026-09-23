using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Home intros and Statistics are CMS-backed. Shared entity collections retain their canonical sources.
/// Editable content is read on every request/navigation, never held in the static snapshot.
/// </summary>
public sealed class HomeContentService(IProjectContentService projectCatalog, IMemberContentService memberCatalog,
    IHomeHeroContentService heroContent, IHomeWelcomeContentService welcomeContent,
    IHomeServicesSectionContentService servicesContent, IHomeProjectsSectionContentService projectsContent,
    IHomeTeamSectionContentService teamContent, IHomeStatisticsContentService statisticsContent,
    IHomePartnersSectionContentService partnersContent,
    IHomeTestimonialsSectionContentService testimonialsContent,
    ITestimonialContentService testimonials, IPartnerContentService partners, IServiceContentService services) : IHomeContentService
{
    private readonly Lazy<Task<HomeContent>> _content = new(() => LoadAsync());

    public async Task<HomeContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var content = await _content.Value.WaitAsync(cancellationToken);
        return content with
        {
            Hero = await heroContent.GetAsync(cancellationToken),
            Welcome = await welcomeContent.GetAsync(cancellationToken),
            ServicesHeading = await servicesContent.GetAsync(cancellationToken),
            Services = await services.GetHomeFeaturedAsync(cancellationToken),
            Projects = await projectCatalog.GetHomeFeaturedAsync(cancellationToken),
            ProjectsHeading = await projectsContent.GetAsync(cancellationToken),
            Members = await memberCatalog.GetHomeFeaturedAsync(cancellationToken),
            Team = await teamContent.GetAsync(cancellationToken),
            Statistics = await statisticsContent.GetAsync(cancellationToken),
            Partners = await partners.GetAsync(cancellationToken),
            PartnersHeading = await partnersContent.GetAsync(cancellationToken),
            TestimonialBrand = await testimonialsContent.GetAsync(cancellationToken),
            Testimonials = await testimonials.GetAsync(cancellationToken)
        };
    }

    private static Task<HomeContent> LoadAsync()
    {
        return Task.FromResult(new HomeContent(
            HomeHeroDefaults.Content,
            HomeWelcomeDefaults.Content,
            HomeServicesSectionDefaults.Content,
            [],
            HomeProjectsSectionDefaults.Content,
            [],
            HomeTeamSectionDefaults.Content,
            [],
            HomeStatisticsDefaults.Content,
            HomePartnersSectionDefaults.Content, [],
            [], HomeTestimonialsSectionDefaults.Content));
    }

}
