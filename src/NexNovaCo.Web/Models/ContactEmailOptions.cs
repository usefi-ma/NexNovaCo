using MimeKit;

namespace NexNovaCo.Web.Models;

// Operational configuration only. Never persisted in CMS or sent to the browser.
public sealed class ContactEmailOptions
{
    public bool Enabled { get; set; }
    public string RecipientAddress { get; set; } = "";
    public string SenderAddress { get; set; } = "";
    public string SenderName { get; set; } = "NexNovaCo";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 15;

    public IReadOnlyList<string> ConfigurationErrors()
    {
        var errors = new List<string>();
        if (!ContactEmailSafety.IsMailbox(RecipientAddress)) errors.Add(nameof(RecipientAddress));
        if (!ContactEmailSafety.IsMailbox(SenderAddress)) errors.Add(nameof(SenderAddress));
        if (SenderName.Length > 100 || ContactEmailSafety.HasHeaderControls(SenderName)) errors.Add(nameof(SenderName));
        if (string.IsNullOrWhiteSpace(SmtpHost) || SmtpHost.Length > 253 || SmtpHost.Any(char.IsWhiteSpace) ||
            Uri.CheckHostName(SmtpHost) == UriHostNameType.Unknown) errors.Add(nameof(SmtpHost));
        if (SmtpPort is < 1 or > 65535) errors.Add(nameof(SmtpPort));
        if (!UseSsl) errors.Add(nameof(UseSsl)); // No plaintext or opportunistic downgrade.
        if (string.IsNullOrWhiteSpace(Username)) errors.Add(nameof(Username));
        if (string.IsNullOrWhiteSpace(Password)) errors.Add(nameof(Password));
        if (TimeoutSeconds is < 5 or > 60) errors.Add(nameof(TimeoutSeconds));
        return errors;
    }
}

public static class ContactEmailSafety
{
    public static bool HasHeaderControls(string value) => value.Any(char.IsControl);
    public static bool IsMailbox(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 254 || HasHeaderControls(value)) return false;
        var address = value.Trim();
        return !address.Any(char.IsWhiteSpace) && MailboxAddress.TryParse(address, out var mailbox) && mailbox is not null &&
            string.IsNullOrEmpty(mailbox.Name) && mailbox.Address == address && address.Contains('@') &&
            address[(address.LastIndexOf('@') + 1)..].Contains('.') && !address.EndsWith('.');
    }
}
