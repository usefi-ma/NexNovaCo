using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Auth.Tests;

// Available only in the test executable, never registered by the production application.
internal static class ContactBrowserHost
{
    public static async Task RunAsync()
    {
        await using var root = new AuthFactory(AuthChecks.NewPassword());
        await using var app = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IContactEmailSender>(new TestContactEmailSender(allowSuccess: true))));
        app.UseKestrel(options => options.ListenLocalhost(5199));
        using var client = app.CreateClient();
        Console.WriteLine("Contact browser test host: http://localhost:5199 — FAKE EMAIL ONLY; subject simulate-failure rejects; other valid subjects succeed after delay.");
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class TestContactEmailSender(bool allowSuccess = false) : IContactEmailSender
{
    public async Task<ContactEmailSendResult> SendAsync(ContactEmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!allowSuccess) return new(false);
        await Task.Delay(1800, cancellationToken);
        Console.WriteLine("Fake Contact sender invoked; no email or message data recorded.");
        return new(message.Subject != "simulate-failure");
    }
}
