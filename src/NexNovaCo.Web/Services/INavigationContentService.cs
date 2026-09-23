using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface INavigationContentService
{
    Task<IReadOnlyList<NavigationListItem>> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NavigationListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<NavigationEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(NavigationEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, NavigationEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
