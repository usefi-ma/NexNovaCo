using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeProjectsSectionContentService
{
    Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeProjectsSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeProjectsSectionEditModel model, CancellationToken cancellationToken = default);
}
