using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IMemberContentService : IMemberCatalog
{
    Task<IReadOnlyList<TeamMemberSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default);
    Task SaveHomeFeaturedAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemberListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<MemberEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(MemberEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, MemberEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
