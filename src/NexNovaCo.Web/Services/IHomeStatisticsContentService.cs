using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeStatisticsContentService
{
    Task<IReadOnlyList<Statistic>> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeStatisticsEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeStatisticsEditModel model, CancellationToken cancellationToken = default);
}
