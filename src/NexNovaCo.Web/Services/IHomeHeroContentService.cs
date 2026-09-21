using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeHeroContentService
{
    Task<HomeHeroContent> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeHeroEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeHeroEditModel model, CancellationToken cancellationToken = default);
}
