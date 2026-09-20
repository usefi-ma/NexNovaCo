using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IContactFormService
{
    Task<ContactFormResult> SubmitAsync(ContactFormModel submission, CancellationToken cancellationToken = default);
}
