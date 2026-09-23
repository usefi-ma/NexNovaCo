using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface ISocialLinkContentService
{
    Task<IReadOnlyList<SocialLinkListItem>> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SocialLinkListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<SocialLinkEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SocialLinkEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, SocialLinkEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
