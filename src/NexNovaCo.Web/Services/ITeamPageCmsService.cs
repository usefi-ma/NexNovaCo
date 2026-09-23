using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface ITeamPageCmsService
{
    Task<TeamContent> ReadPublicAsync(CancellationToken ct = default);
    Task<TeamHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default);
    Task SaveHeroAsync(TeamHeroEditModel model, CancellationToken ct = default);
    Task<TeamSectionEditModel> GetSectionForEditAsync(CancellationToken ct = default);
    Task SaveSectionAsync(TeamSectionEditModel model, CancellationToken ct = default);
}
