using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class ContactFormModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100), RegularExpression(@"^[^\p{Cc}]*$", ErrorMessage = "First name must be a single line.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100), RegularExpression(@"^[^\p{Cc}]*$", ErrorMessage = "Last name must be a single line.")]
    public string LastName { get; set; } = "";

    private string _email = "";
    // Trim harmless surrounding spaces, but never erase CR/LF before validation.
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(254), ContactMailbox]
    public string Email { get => _email; set => _email = value?.Trim(' ', '\t') ?? ""; }

    [StringLength(200), RegularExpression(@"^[^\p{Cc}]*$", ErrorMessage = "Subject must be a single line.")]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(5000)]
    public string Message { get; set; } = "";

    public string Website { get; set; } = "";
    public void Reset() => FirstName = LastName = Email = Subject = Message = Website = "";
}

public sealed class ContactMailboxAttribute : ValidationAttribute
{
    public ContactMailboxAttribute() => ErrorMessage = "Please enter a valid email address.";
    public override bool IsValid(object? value) => value is null || value is string text &&
        (string.IsNullOrWhiteSpace(text) || ContactEmailSafety.IsMailbox(text));
}

public sealed record ContactFormResult(bool Succeeded, string Message);
