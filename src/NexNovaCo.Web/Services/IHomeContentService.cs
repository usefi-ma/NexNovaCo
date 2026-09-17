using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IHomeContentService
{
    Task<HomeContent> GetAsync(CancellationToken cancellationToken = default);
}
