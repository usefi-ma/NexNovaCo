using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IMemberCatalog
{
    Task<IReadOnlyList<TeamMemberSummary>> GetAsync(CancellationToken cancellationToken = default);
    Task<MemberDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default);
}
