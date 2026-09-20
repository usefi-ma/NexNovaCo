using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public sealed class ProjectsContentService(IProjectCatalog projects) : IProjectsContentService
{
    private readonly Lazy<Task<ProjectsContent>> _content = new(() => LoadAsync(projects));

    public Task<ProjectsContent> GetAsync(CancellationToken cancellationToken = default)
        => _content.Value.WaitAsync(cancellationToken);

    private static async Task<ProjectsContent> LoadAsync(IProjectCatalog projects) => new(
        new("Our Projects", "Our Projects",
            "From AI-powered platforms to custom enterprise web solutions, we specialize in bringing digital ideas to life. Each project reflects our focus on innovation, user experience, and scalable performance.",
            "Explore Our Works", "projects#Project"),
        await projects.GetAsync(), TestimonialCatalog.All, TestimonialCatalog.Brand);
}
