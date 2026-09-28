using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IShopContentService
{
    Task<ShopContent> ReadPublicAsync(CancellationToken cancellationToken = default);
    Task<ShopHeroEditModel> GetHeroForEditAsync(CancellationToken cancellationToken = default);
    Task SaveHeroAsync(ShopHeroEditModel model, CancellationToken cancellationToken = default);
    Task<ShopProductsSectionEditModel> GetProductsSectionForEditAsync(CancellationToken cancellationToken = default);
    Task SaveProductsSectionAsync(ShopProductsSectionEditModel model, CancellationToken cancellationToken = default);
}
