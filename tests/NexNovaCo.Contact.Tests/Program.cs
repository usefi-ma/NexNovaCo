using System.ComponentModel.DataAnnotations;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}
static ContactFormModel Valid() => new() {
    FirstName = "Demo", LastName = "Tester", Email = "demo@example.com", Message = "Local demo test."
};
static List<ValidationResult> Errors(ContactFormModel model)
{
    var errors = new List<ValidationResult>();
    Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true);
    return errors;
}

var empty = new ContactFormModel();
Check(empty.Message == "" && empty.Subject == "", "Textarea and optional subject must start empty.");
Check(Errors(empty).Count == 4, "Exactly four required fields must reject an empty form.");
foreach (var field in new[] { nameof(empty.FirstName), nameof(empty.LastName), nameof(empty.Email), nameof(empty.Message) })
{
    var model = Valid();
    typeof(ContactFormModel).GetProperty(field)!.SetValue(model, " \t\r\n ");
    Check(Errors(model).Any(error => error.MemberNames.Contains(field)), $"Whitespace-only {field} must fail.");
}
foreach (var email in new[] { "not-an-email", "demo@example", "a b@example.com", "a@@example.com", "a@example." })
{
    var model = Valid(); model.Email = email;
    Check(Errors(model).Any(error => error.MemberNames.Contains(nameof(model.Email))), $"Invalid email accepted: {email}");
}
var valid = Valid(); valid.Email = "  demo@example.com  ";
Check(valid.Email == "demo@example.com" && Errors(valid).Count == 0, "Trim email as in the approved source.");
Check(Errors(Valid()).Count == 0, "Blank subject must remain optional.");
valid.Subject = "  ";
Check(Errors(valid).Count == 0, "Whitespace-only optional subject is allowed.");

var service = new DemoContactFormService();
var result = await service.SubmitAsync(valid);
Check(result.Succeeded && result.Message == "Demo form submitted successfully. No message was sent.", "Confirmation must explicitly disclaim delivery.");
Check(valid.Message == "Local demo test.", "Service must not mutate the form; UI owns reset.");
try { await service.SubmitAsync(new()); throw new InvalidOperationException("Service accepted invalid input."); }
catch (ValidationException) { checks++; }
try { await service.SubmitAsync(valid, new CancellationToken(true)); throw new InvalidOperationException("Cancellation ignored."); }
catch (OperationCanceledException) { checks++; }
valid.Reset();
Check(new[] { valid.FirstName, valid.LastName, valid.Email, valid.Subject, valid.Message }.All(value => value == ""), "Reset must clear every field.");
Check(Errors(valid).Count == 4, "A reset form must require fresh valid input.");
Check((await service.SubmitAsync(Valid())).Succeeded, "Repeated demo submissions must be independent.");
Console.WriteLine($"PASS: {checks} Contact model/service checks (required fields, whitespace, email, optional subject, honest demo result, cancellation and reset).");
