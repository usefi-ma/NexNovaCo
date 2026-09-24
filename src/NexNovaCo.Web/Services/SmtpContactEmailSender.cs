using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public sealed class SmtpContactEmailSender(IOptions<ContactEmailOptions> options,
    Func<ISmtpClient> createClient, ILogger<SmtpContactEmailSender> logger) : IContactEmailSender
{
    public async Task<ContactEmailSendResult> SendAsync(ContactEmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var settings = options.Value;
            if (!settings.Enabled)
            {
                logger.LogWarning("Contact email unavailable: {Reason}", "Disabled");
                return new(false);
            }
            var errors = settings.ConfigurationErrors();
            if (errors.Count != 0)
            {
                logger.LogWarning("Contact email configuration invalid; required settings: {SettingNames}", string.Join(", ", errors));
                return new(false);
            }
            using var email = CreateMessage(settings, message);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            using var smtp = createClient(); // A fresh client per send; no protocol/PII logger.
            smtp.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
            smtp.Timeout = settings.TimeoutSeconds * 1000;
            await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort,
                settings.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, timeout.Token);
            await smtp.AuthenticateAsync(settings.Username, settings.Password, timeout.Token);
            await smtp.SendAsync(email, timeout.Token);
            // Once accepted by SMTP, a QUIT/network failure must not turn success into a retry prompt.
            try { await smtp.DisconnectAsync(true, timeout.Token); }
            catch (Exception exception) { logger.LogDebug("Contact SMTP cleanup: {Category}", exception.GetType().Name); }
            logger.LogInformation("Contact email accepted by transport");
            return new(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // SMTP exception messages can contain server replies, addresses and message data.
            // Log only category, never exception object/message or submitted content.
            logger.LogWarning("Contact email transport failed: {Category}", exception is OperationCanceledException ? "Timeout" : exception.GetType().Name);
            return new(false);
        }
    }

    public static MimeMessage CreateMessage(ContactEmailOptions settings, ContactEmailMessage message)
    {
        if (settings.ConfigurationErrors().Count != 0) throw new InvalidOperationException("Invalid email configuration.");
        var form = new ContactFormModel { FirstName = message.FirstName, LastName = message.LastName,
            Email = message.Email, Subject = message.Subject, Message = message.Message };
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(form,
            new System.ComponentModel.DataAnnotations.ValidationContext(form), true);
        var subject = string.IsNullOrWhiteSpace(message.Subject) ? "NexNovaCo Contact Request" : message.Subject.Trim();
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(settings.SenderName.Trim(), settings.SenderAddress.Trim()));
        email.To.Add(new MailboxAddress("", settings.RecipientAddress.Trim()));
        email.ReplyTo.Add(new MailboxAddress("", message.Email.Trim()));
        email.Subject = subject;
        email.Date = message.SubmittedAtUtc;
        email.Body = new TextPart("plain") { Text = $"New NexNovaCo Contact Request\n\nName: {message.FirstName.Trim()} {message.LastName.Trim()}\nEmail: {message.Email.Trim()}\nSubject: {subject}\nSubmitted: {message.SubmittedAtUtc:O}\n\nMessage:\n{message.Message.Trim()}" };
        return email;
    }
}
