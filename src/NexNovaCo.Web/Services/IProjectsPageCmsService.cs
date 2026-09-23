using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IProjectsPageCmsService
{
    Task<ProjectsContent> ReadPublicAsync(CancellationToken ct = default);
    Task<ProjectsHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default);
    Task SaveHeroAsync(ProjectsHeroEditModel model, CancellationToken ct = default);
    Task<ProjectsTestimonialsEditModel> GetTestimonialsForEditAsync(CancellationToken ct = default);
    Task SaveTestimonialsAsync(ProjectsTestimonialsEditModel model, CancellationToken ct = default);
}
