using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public sealed class ServicesContentService(IServiceContentService services, IServicesPageCmsService page) : IServicesContentService
{
    public async Task<ServicesContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var content = await page.ReadPublicAsync(cancellationToken);
        return content with { Services = await services.GetAsync(cancellationToken) };
    }
}
