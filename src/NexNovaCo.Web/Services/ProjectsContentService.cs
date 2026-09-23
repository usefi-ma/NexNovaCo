using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public sealed class ProjectsContentService(IProjectsPageCmsService page, IProjectCatalog projects, ITestimonialContentService testimonials) : IProjectsContentService
{
    public async Task<ProjectsContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var content = await page.ReadPublicAsync(cancellationToken);
        return content with { Projects = await projects.GetAsync(cancellationToken), Testimonials = await testimonials.GetAsync(cancellationToken) };
    }
}
