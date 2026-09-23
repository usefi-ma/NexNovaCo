using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public sealed class AboutContentService(IAboutCmsService about, IPartnerContentService partners) : IAboutContentService
{
    public async Task<AboutContent> GetAsync(CancellationToken cancellationToken = default) =>
        (await about.ReadPublicAsync(cancellationToken)) with { Partners = await partners.GetAsync(cancellationToken) };
}
