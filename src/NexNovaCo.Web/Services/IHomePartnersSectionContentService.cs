using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomePartnersSectionContentService
{
    Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default);
    Task<HomePartnersSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomePartnersSectionEditModel model, CancellationToken cancellationToken = default);
}
