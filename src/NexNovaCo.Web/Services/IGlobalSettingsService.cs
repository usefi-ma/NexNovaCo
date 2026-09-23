using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IGlobalSettingsService
{
    Task<SiteIdentityEditModel> ReadSiteIdentityAsync(CancellationToken ct = default);
    Task<SiteIdentityEditModel> GetSiteIdentityForEditAsync(CancellationToken ct = default);
    Task SaveSiteIdentityAsync(SiteIdentityEditModel model, CancellationToken ct = default);
    Task<FooterEditModel> ReadFooterAsync(CancellationToken ct = default);
    Task<FooterEditModel> GetFooterForEditAsync(CancellationToken ct = default);
    Task SaveFooterAsync(FooterEditModel model, CancellationToken ct = default);
}
