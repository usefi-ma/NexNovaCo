using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface ITestimonialContentService
{
    Task<IReadOnlyList<Testimonial>> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TestimonialListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<TestimonialEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(TestimonialEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, TestimonialEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
