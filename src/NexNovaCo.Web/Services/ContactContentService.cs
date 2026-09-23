using NexNovaCo.Web.Models;
namespace NexNovaCo.Web.Services;

public sealed class ContactContentService(IContactPageCmsService settings) : IContactContentService
{
    public Task<ContactContent> GetAsync(CancellationToken cancellationToken = default) => settings.ReadPublicAsync(cancellationToken);
}
