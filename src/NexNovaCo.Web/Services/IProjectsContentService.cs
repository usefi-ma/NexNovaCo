using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IProjectsContentService
{
    Task<ProjectsContent> GetAsync(CancellationToken cancellationToken = default);
}
