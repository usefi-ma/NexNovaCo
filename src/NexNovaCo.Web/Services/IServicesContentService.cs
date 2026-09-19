using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IServicesContentService
{
    Task<ServicesContent> GetAsync(CancellationToken cancellationToken = default);
}
