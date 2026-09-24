namespace NexNovaCo.Web.Services;

public sealed record ContactEmailMessage(string FirstName, string LastName, string Email, string Subject,
    string Message, DateTimeOffset SubmittedAtUtc);
public sealed record ContactEmailSendResult(bool Succeeded);

public interface IContactEmailSender
{
    Task<ContactEmailSendResult> SendAsync(ContactEmailMessage message, CancellationToken cancellationToken = default);
}
