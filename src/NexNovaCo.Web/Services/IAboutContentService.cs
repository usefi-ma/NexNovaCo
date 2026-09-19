using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IAboutContentService
{
    Task<AboutContent> GetAsync(CancellationToken cancellationToken = default);
}
