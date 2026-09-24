using System.ComponentModel.DataAnnotations;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Scoped to one Interactive Server circuit. No submission persistence or IP/header trust.
public sealed class ContactFormService(IContactEmailSender sender, ContactSubmissionLimiter limiter,
    ILogger<ContactFormService> logger) : IContactFormService, IDisposable
{
    public const string FailureMessage = "We couldn't send your message right now. Please try again later.";
    public const string SuccessMessage = "Thank you. Your message has been sent successfully.";
    private readonly System.Threading.RateLimiting.FixedWindowRateLimiter _circuit =
        ContactSubmissionLimiter.Window(5, TimeSpan.FromMinutes(10));
    private int _sending;

    public async Task<ContactFormResult> SubmitAsync(ContactFormModel submission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _sending, 1, 0) != 0) return new(false, FailureMessage);
        try
        {
            using var circuit = _circuit.AttemptAcquire();
            using var global = limiter.AcquireAttempt();
            if (!circuit.IsAcquired || !global.IsAcquired)
            {
                logger.LogWarning("Contact submission rejected: {Reason}", "RateLimit");
                return new(false, FailureMessage);
            }
            if (!string.IsNullOrEmpty(submission.Website))
            {
                logger.LogWarning("Contact submission rejected: {Reason}", "Honeypot");
                return new(false, FailureMessage);
            }
            if (!Validator.TryValidateObject(submission, new ValidationContext(submission), [], true))
                return new(false, FailureMessage);
            // Snapshot validated values before awaiting I/O; the mutable UI model is never retained.
            var message = new ContactEmailMessage(submission.FirstName.Trim(), submission.LastName.Trim(),
                submission.Email.Trim(), submission.Subject.Trim(), submission.Message.Trim(), DateTimeOffset.UtcNow);
            using var send = limiter.AcquireSend();
            if (!send.IsAcquired)
            {
                logger.LogWarning("Contact submission rejected: {Reason}", "ConcurrencyLimit");
                return new(false, FailureMessage);
            }
            var result = await sender.SendAsync(message, cancellationToken);
            return new(result.Succeeded, result.Succeeded ? SuccessMessage : FailureMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning("Contact submission failed: {Category}", exception.GetType().Name);
            return new(false, FailureMessage);
        }
        finally { Volatile.Write(ref _sending, 0); }
    }
    public void Dispose() => _circuit.Dispose();
}
