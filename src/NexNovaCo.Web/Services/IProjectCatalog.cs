using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IProjectCatalog
{
    Task<IReadOnlyList<ProjectSummary>> GetAsync(CancellationToken cancellationToken = default);
    Task<ProjectDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default);
}
