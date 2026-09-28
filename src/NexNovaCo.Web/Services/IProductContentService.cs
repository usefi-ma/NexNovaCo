using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IProductContentService
{
    Task<IReadOnlyList<ProductSummary>> GetAsync(CancellationToken cancellationToken = default);
    Task<ProductDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSelectionItem>> ListChoicesAsync(int? exceptId = null, CancellationToken cancellationToken = default);
    Task<ProductEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, ProductEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
