using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeServicesSectionContentService
{
    Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeServicesSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeServicesSectionEditModel model, CancellationToken cancellationToken = default);
}
