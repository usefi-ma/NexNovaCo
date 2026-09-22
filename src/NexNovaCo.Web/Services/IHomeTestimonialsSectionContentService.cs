using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeTestimonialsSectionContentService
{
    Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeTestimonialsSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeTestimonialsSectionEditModel model, CancellationToken cancellationToken = default);
}
