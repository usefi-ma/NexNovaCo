using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IPartnerContentService
{
    Task<IReadOnlyList<Partner>> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartnerListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<PartnerEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(PartnerEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, PartnerEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
