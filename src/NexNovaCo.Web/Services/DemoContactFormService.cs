using System.ComponentModel.DataAnnotations;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Replace through DI only after a real delivery service is explicitly approved.
// No HTTP, email, storage, logging of submitted values, or retained submission state.
public sealed class DemoContactFormService : IContactFormService
{
    public Task<ContactFormResult> SubmitAsync(ContactFormModel submission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validator.ValidateObject(submission, new ValidationContext(submission), validateAllProperties: true);
        return Task.FromResult(new ContactFormResult(true,
            "Demo form submitted successfully. No message was sent."));
    }
}
