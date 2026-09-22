using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeTeamSectionContentService
{
    Task<TeamSectionContent> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeTeamSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeTeamSectionEditModel model, CancellationToken cancellationToken = default);
}
