using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Team editorial settings compose with the canonical shared member catalog.
public sealed class TeamContentService(ITeamPageCmsService page, IMemberCatalog memberCatalog) : ITeamContentService
{
    public async Task<TeamContent> GetAsync(CancellationToken cancellationToken = default)
        => (await page.ReadPublicAsync(cancellationToken)) with { Members = await memberCatalog.GetAsync(cancellationToken) };
}
