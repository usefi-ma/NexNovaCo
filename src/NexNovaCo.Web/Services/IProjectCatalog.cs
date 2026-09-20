using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IProjectCatalog
{
    Task<IReadOnlyList<ProjectSummary>> GetAsync(CancellationToken cancellationToken = default);
}
