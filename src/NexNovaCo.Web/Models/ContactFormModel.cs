using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class ContactFormModel
{
    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = "";

    private string _email = "";
    // Match the cleaned source: trim email, reject whitespace and require a dotted domain.
    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "Please enter a valid email address.")]
    public string Email { get => _email; set => _email = value?.Trim() ?? ""; }

    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Message is required.")]
    public string Message { get; set; } = "";

    public void Reset() => FirstName = LastName = Email = Subject = Message = "";
}

public sealed record ContactFormResult(bool Succeeded, string Message);
