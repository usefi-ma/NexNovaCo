using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IServiceContentService
{
    Task<IReadOnlyList<ServiceSummary>> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default);
    Task SaveHomeFeaturedAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<ServiceEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ServiceEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, ServiceEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
