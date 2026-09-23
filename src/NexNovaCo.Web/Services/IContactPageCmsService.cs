using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IContactPageCmsService
{
    Task<ContactContent> ReadPublicAsync(CancellationToken ct = default);
    Task<SiteContactContent> ReadSiteContactAsync(CancellationToken ct = default);
    Task<ContactHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default);
    Task SaveHeroAsync(ContactHeroEditModel model, CancellationToken ct = default);
    Task<ContactInfoEditModel> GetInfoForEditAsync(CancellationToken ct = default);
    Task SaveInfoAsync(ContactInfoEditModel model, CancellationToken ct = default);
    Task<ContactFormEditModel> GetFormForEditAsync(CancellationToken ct = default);
    Task SaveFormAsync(ContactFormEditModel model, CancellationToken ct = default);
    Task<ContactMapEditModel> GetMapForEditAsync(CancellationToken ct = default);
    Task SaveMapAsync(ContactMapEditModel model, CancellationToken ct = default);
}
