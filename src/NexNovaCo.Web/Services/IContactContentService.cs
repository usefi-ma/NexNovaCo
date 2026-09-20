using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IContactContentService
{
    Task<ContactContent> GetAsync(CancellationToken cancellationToken = default);
}
