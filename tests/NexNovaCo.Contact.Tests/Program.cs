using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

var checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
static ContactFormModel Valid() => new() { FirstName = "  Visitor  ", LastName = " Tester ", Email = " visitor@example.invalid ", Message = "  First line\r\nSecond line  " };
static ContactEmailOptions Config() => new() { Enabled = true, RecipientAddress = "recipient@example.invalid", SenderAddress = "sender@example.invalid", SmtpHost = "smtp.example.invalid", Username = "test-user", Password = "test-password" };
static List<ValidationResult> Errors(ContactFormModel model) { var list = new List<ValidationResult>(); Validator.TryValidateObject(model, new(model), list, true); return list; }
Check(Errors(new()).Count == 4, "Four required fields");
foreach (var field in new[] { "FirstName", "LastName", "Email", "Message" }) {
    var model = Valid(); typeof(ContactFormModel).GetProperty(field)!.SetValue(model, " \r\n ");
    Check(Errors(model).Any(x => x.MemberNames.Contains(field)), "Whitespace rejected: " + field);
}
foreach (var email in new[] { "invalid", "a@localhost", "a b@example.com", "a@@example.com", "a@example.", "a@example.com\r\nBcc:other@example.com", "Name <a@example.com>", "a@example.com,b@example.com", "a@example.com\r\n" }) {
    var model = Valid(); model.Email = email; Check(Errors(model).Count > 0, "Email/header input rejected");
}
foreach (var (field, length) in new[] { ("FirstName", 101), ("LastName", 101), ("Email", 255), ("Subject", 201), ("Message", 5001) }) {
    var model = Valid(); typeof(ContactFormModel).GetProperty(field)!.SetValue(model, new string('x', length));
    Check(Errors(model).Any(x => x.MemberNames.Contains(field)), "Length enforced: " + field);
}
foreach (var field in new[] { "FirstName", "LastName", "Subject" }) {
    var model = Valid(); typeof(ContactFormModel).GetProperty(field)!.SetValue(model, "Header\r\nBcc:bad@example.invalid"); Check(Errors(model).Count > 0, "CRLF rejected");
}
var fake = new FakeSender(); var logs = new CaptureLogger<ContactFormService>();
using var limiter = new ContactSubmissionLimiter();
using var service = new ContactFormService(fake, limiter, logs);
var valid = Valid(); var result = await service.SubmitAsync(valid);
Check(result.Succeeded && result.Message == ContactFormService.SuccessMessage && fake.Calls == 1, "One send before success");
Check(fake.Last!.FirstName == "Visitor" && fake.Last.LastName == "Tester" && fake.Last.Email == "visitor@example.invalid", "Normalize surrounding whitespace");
Check(fake.Last.Message == "First line\r\nSecond line" && valid.Message.StartsWith("  "), "Preserve message lines, do not mutate form");
Check(fake.Last.SubmittedAtUtc.Offset == TimeSpan.Zero, "UTC timestamp");
var options = Config();
using (var mime = SmtpContactEmailSender.CreateMessage(options, fake.Last)) {
    Check(mime.From.Mailboxes.Single().Address == options.SenderAddress, "Configured From, not visitor");
    Check(mime.To.Mailboxes.Single().Address == options.RecipientAddress, "Configured To");
    Check(mime.ReplyTo.Mailboxes.Single().Address == "visitor@example.invalid", "Visitor Reply-To");
    Check(mime.Subject == "NexNovaCo Contact Request", "Blank subject fallback");
    Check(mime.TextBody?.Contains("First line\r\nSecond line") == true && mime.HtmlBody is null, "Plain text only");
}
foreach (var bad in new[] { new ContactFormModel(), new ContactFormModel { FirstName="V", LastName="T", Email="not-email", Message="x" }, new ContactFormModel { FirstName="V",LastName="T",Email="v@example.invalid",Message=new string('x',5001) } }) {
    using var s = new ContactFormService(fake, limiter, logs); var before = fake.Calls;
    Check(!(await s.SubmitAsync(bad)).Succeeded && fake.Calls == before, "Server validation: no send");
}
var trap = Valid(); trap.Website = "bot";
Check(!(await service.SubmitAsync(trap)).Succeeded && fake.Calls == 1, "Honeypot: no send");
fake.Success = false;
Check(!(await service.SubmitAsync(valid)).Succeeded && valid.Message.StartsWith("  "), "Failure retains values");
fake.Throw = true;
Check((await service.SubmitAsync(valid)).Message == ContactFormService.FailureMessage, "Unexpected sender exception is generic");
fake.Throw = false; fake.Success = true;
using (var limits = new ContactSubmissionLimiter()) {
    var limitedSender = new FakeSender(); using var limited = new ContactFormService(limitedSender, limits, logs);
    for(var i=0;i<5;i++) Check((await limited.SubmitAsync(Valid())).Succeeded, "Normal circuit budget");
    Check(!(await limited.SubmitAsync(Valid())).Succeeded && limitedSender.Calls==5, "Sixth circuit attempt rejected");
}
using (var limits = new ContactSubmissionLimiter()) {
    var sender = new FakeSender();
    for(var i=0;i<30;i++) { using var s = new ContactFormService(sender, limits, logs); Check((await s.SubmitAsync(Valid())).Succeeded, "Process budget across circuits"); }
    using var blocked = new ContactFormService(sender, limits, logs);
    Check(!(await blocked.SubmitAsync(Valid())).Succeeded && sender.Calls==30, "New circuits cannot bypass process budget");
}
using (var limits = new ContactSubmissionLimiter()) {
    var sender = new FakeSender { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
    using var s = new ContactFormService(sender, limits, logs);
    var first = s.SubmitAsync(Valid()); Check(!first.IsCompleted, "Waits for actual send result");
    Check(!(await s.SubmitAsync(Valid())).Succeeded && sender.Calls==1, "Concurrent duplicate blocked");
    using var second = new ContactFormService(sender, limits, logs); var secondTask = second.SubmitAsync(Valid());
    using var third = new ContactFormService(sender, limits, logs); Check(!(await third.SubmitAsync(Valid())).Succeeded && sender.Calls==2, "Only two concurrent sends");
    sender.Gate.SetResult(); Check((await first).Succeeded && (await secondTask).Succeeded, "Completes only after fake accepted");
}
using (var limits = new ContactSubmissionLimiter()) {
    using var s = new ContactFormService(fake, limits, logs);
    try { await s.SubmitAsync(Valid(), new(true)); throw new Exception("Cancellation ignored"); } catch(OperationCanceledException) { checks++; }
}
valid.Website="trap"; valid.Reset(); Check(new[] {valid.FirstName,valid.LastName,valid.Email,valid.Subject,valid.Message,valid.Website}.All(x=>x==""), "Reset clears all six values");
Check(Errors(valid).Count==4, "Reset needs fresh valid input");
Check(!logs.Text.Contains("First line") && !logs.Text.Contains("visitor@example") && logs.Text.Contains("Honeypot") && logs.Text.Contains("RateLimit"), "Structured rejection logging without PII");

// MailKit interface proxy: these tests cannot open a network socket.
var client = DispatchProxy.Create<ISmtpClient, SmtpProxy>(); var proxy = (SmtpProxy)(object)client;
var transportLogs = new CaptureLogger<SmtpContactEmailSender>();
SmtpContactEmailSender Transport(ContactEmailOptions config) => new(Options.Create(config), () => client, transportLogs);
var message = new ContactEmailMessage("Visitor", "Tester", "visitor@example.invalid", " Hello ", "PRIVATE_BODY", DateTimeOffset.UtcNow);
Check((await Transport(options).SendAsync(message)).Succeeded, "Fake SMTP accepted");
Check(proxy.Mode==SecureSocketOptions.StartTls && proxy.Timeout==15000 && proxy.Sends==1, "Required STARTTLS, bounded timeout, one async send");
Check(proxy.Subject=="Hello" && proxy.From==options.SenderAddress && proxy.To==options.RecipientAddress && proxy.ReplyTo==message.Email, "Transport actual MIME headers");
options.SmtpPort=465;Check((await Transport(options).SendAsync(message)).Succeeded && proxy.Mode==SecureSocketOptions.SslOnConnect,"Implicit TLS 465");
var calls=proxy.Sends; options.UseSsl=false; Check(!(await Transport(options).SendAsync(message)).Succeeded&&proxy.Sends==calls,"Plaintext configuration rejected");
options.UseSsl=true;options.Enabled=false;Check(!(await Transport(options).SendAsync(message)).Succeeded&&proxy.Sends==calls,"Disabled: no socket/send");
Check(!(await Transport(new() { Enabled=true }).SendAsync(message)).Succeeded,"Missing config is graceful");
foreach(var field in new[]{"RecipientAddress","SenderAddress","SenderName"}) { var c=Config();typeof(ContactEmailOptions).GetProperty(field)!.SetValue(c,"bad\r\nBcc:x@example.invalid");Check(c.ConfigurationErrors().Contains(field),"Configuration header injection rejected"); }
var badMessage=message with { Subject="bad\r\nBcc:other@example.invalid" };Check(!(await Transport(Config()).SendAsync(badMessage)).Succeeded&&proxy.Sends==calls,"Sender independently rejects header injection");
proxy.Failure="SendAsync";Check(!(await Transport(Config()).SendAsync(message)).Succeeded,"SMTP failure is generic");
proxy.Failure="ConnectAsync";Check(!(await Transport(Config()).SendAsync(message)).Succeeded,"Connection/TLS failure is generic");
proxy.Failure="AuthenticateAsync";Check(!(await Transport(Config()).SendAsync(message)).Succeeded,"Auth failure is generic");
proxy.Failure="Timeout";Check(!(await Transport(Config()).SendAsync(message)).Succeeded,"Timeout is generic");
proxy.Failure="DisconnectAsync";Check((await Transport(Config()).SendAsync(message)).Succeeded,"Accepted send not reversed by QUIT failure");
Check(!transportLogs.Text.Contains("PRIVATE_BODY")&&!transportLogs.Text.Contains("test-password")&&!transportLogs.Text.Contains("visitor@example")&&!transportLogs.Text.Contains("SMTP_PRIVATE"),"Never log SMTP exception details/credentials/body");
// Exercise the real component's submit handler/state without adding a UI test framework.
static object? Field(object instance,string name) => instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(instance);
static Task Submit(object instance) => (Task)instance.GetType().GetMethod("SubmitAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(instance,null)!;
using (var limits = new ContactSubmissionLimiter()) {
    var sender = new FakeSender { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
    using var forms = new ContactFormService(sender,limits,logs);
    using var component = new NexNovaCo.Web.Components.Sections.Contact.ContactForm();
    var type=component.GetType(); type.GetProperty("FormService",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(component,forms);
    type.GetMethod("OnInitialized",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(component,null);
    var model=(ContactFormModel)Field(component,"_model")!;
    model.FirstName="Visitor";model.LastName="Tester";model.Email="visitor@example.invalid";model.Message="Keep until accepted";
    var pending=Submit(component);
    Check((bool)Field(component,"_sending")! && (string)Field(component,"_status")! == "Sending your message…","Component loading state before send completes");
    await Submit(component);Check(sender.Calls==1,"Component duplicate handler cannot resend");
    sender.Gate.SetResult();await pending;
    Check(model.Message==""&&model.Website==""&&(string)Field(component,"_status")! == ContactFormService.SuccessMessage,"Component resets only after successful send");
    Check(!(bool)Field(component,"_sending")! && !(bool)Field(component,"_hasErrors")! && (bool)Field(component,"_focusFeedback")!,"Component restores submit and requests accessible success focus");
    model.FirstName="Visitor";model.LastName="Tester";model.Email="visitor@example.invalid";model.Message="Retain on failure";sender.Success=false;
    await Submit(component);Check(model.Message=="Retain on failure"&&(bool)Field(component,"_hasErrors")!&&!(bool)Field(component,"_sending")!,"Component failure retains values and restores submit");
    model.Website="trap";var before=sender.Calls;await Submit(component);Check(sender.Calls==before&&model.Website=="trap","Component honeypot never sends or fakes success");
}
Console.WriteLine($"PASS: {checks} Contact delivery/security checks. Fake sender/SMTP proxy only; no real email.");

sealed class FakeSender : IContactEmailSender {
    public int Calls; public bool Success=true,Throw; public ContactEmailMessage? Last; public TaskCompletionSource? Gate;
    public async Task<ContactEmailSendResult> SendAsync(ContactEmailMessage m,CancellationToken ct=default){Calls++;Last=m;if(Gate is not null)await Gate.Task.WaitAsync(ct);if(Throw)throw new IOException("PRIVATE_BODY");return new(Success);}
}
sealed class CaptureLogger<T> : ILogger<T> {
    public string Text=""; public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
    public bool IsEnabled(LogLevel level)=>true;
    public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)=>Text+=formatter(state,exception);
}
public class SmtpProxy : DispatchProxy {
    public SecureSocketOptions Mode; public int Timeout,Sends;public string? Subject,From,To,ReplyTo;public string Failure="";
    protected override object? Invoke(MethodInfo? method,object?[]? args) {
        var name=method!.Name;
        if(name==Failure)throw new IOException("SMTP_PRIVATE test-password PRIVATE_BODY");
        if(Failure=="Timeout"&&name=="ConnectAsync")throw new OperationCanceledException();
        if(name=="set_Timeout") {Timeout=(int)args![0]!;return null;}
        if(name=="ConnectAsync") {Mode=(SecureSocketOptions)args![2]!;return Task.CompletedTask;}
        if(name=="SendAsync") {var m=(MimeMessage)args![0]!;Subject=m.Subject;From=m.From.Mailboxes.Single().Address;To=m.To.Mailboxes.Single().Address;ReplyTo=m.ReplyTo.Mailboxes.Single().Address;Sends++;return Task.FromResult("accepted");}
        if(method.ReturnType==typeof(Task))return Task.CompletedTask;
        return method.ReturnType==typeof(void)?null:method.ReturnType.IsValueType?Activator.CreateInstance(method.ReturnType):null;
    }
}
