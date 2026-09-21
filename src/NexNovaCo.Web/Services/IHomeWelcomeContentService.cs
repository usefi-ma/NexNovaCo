using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeWelcomeContentService
{
    Task<WelcomeContent> GetAsync(CancellationToken cancellationToken = default);
    Task<HomeWelcomeEditModel> GetForEditAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(HomeWelcomeEditModel model, CancellationToken cancellationToken = default);
}
