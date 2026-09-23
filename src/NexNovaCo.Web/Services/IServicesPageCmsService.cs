using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IServicesPageCmsService
{
    Task<ServicesContent> ReadPublicAsync(CancellationToken ct = default);
    Task<ServicesHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default);
    Task SaveHeroAsync(ServicesHeroEditModel model, CancellationToken ct = default);
    Task<ServicesBenefitsEditModel> GetBenefitsForEditAsync(CancellationToken ct = default);
    Task SaveBenefitsAsync(ServicesBenefitsEditModel model, CancellationToken ct = default);
    Task<ServicesProcessEditModel> GetProcessForEditAsync(CancellationToken ct = default);
    Task SaveProcessAsync(ServicesProcessEditModel model, CancellationToken ct = default);
    Task<ServicesPricingEditModel> GetPricingForEditAsync(CancellationToken ct = default);
    Task SavePricingAsync(ServicesPricingEditModel model, CancellationToken ct = default);
    Task<ServicesFaqEditModel> GetFaqForEditAsync(CancellationToken ct = default);
    Task SaveFaqAsync(ServicesFaqEditModel model, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceBenefitItem>> ListBenefitAsync(CancellationToken ct = default);
    Task<ServiceBenefitEditModel> GetBenefitItemForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateBenefitAsync(ServiceBenefitEditModel model, CancellationToken ct = default);
    Task UpdateBenefitAsync(int id, ServiceBenefitEditModel model, CancellationToken ct = default);
    Task DeleteBenefitAsync(int id, CancellationToken ct = default);
    Task ReorderBenefitAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceProcessStepItem>> ListProcessAsync(CancellationToken ct = default);
    Task<ServiceProcessEditModel> GetProcessItemForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateProcessAsync(ServiceProcessEditModel model, CancellationToken ct = default);
    Task UpdateProcessAsync(int id, ServiceProcessEditModel model, CancellationToken ct = default);
    Task DeleteProcessAsync(int id, CancellationToken ct = default);
    Task ReorderProcessAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
    Task<IReadOnlyList<ServicePricingPlanItem>> ListPricingAsync(CancellationToken ct = default);
    Task<ServicePricingEditModel> GetPricingItemForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreatePricingAsync(ServicePricingEditModel model, CancellationToken ct = default);
    Task UpdatePricingAsync(int id, ServicePricingEditModel model, CancellationToken ct = default);
    Task DeletePricingAsync(int id, CancellationToken ct = default);
    Task ReorderPricingAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceFaqItem>> ListFaqAsync(CancellationToken ct = default);
    Task<ServiceFaqEditModel> GetFaqItemForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateFaqAsync(ServiceFaqEditModel model, CancellationToken ct = default);
    Task UpdateFaqAsync(int id, ServiceFaqEditModel model, CancellationToken ct = default);
    Task DeleteFaqAsync(int id, CancellationToken ct = default);
    Task ReorderFaqAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
}
