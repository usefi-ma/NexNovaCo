using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Auth.Tests;

// Test executable only: isolated database and synthetic Identity account, no public auth bypass.
internal static class DetailTemplateBrowserHost
{
    public static async Task RunAsync(string fixturePath, string? databasePath = null)
    {
        var reuseSession = databasePath is not null && File.Exists(databasePath) && File.Exists(fixturePath);
        await using var app = new AuthFactory(AuthChecks.NewPassword(), databasePath: databasePath, publicBaseUrl: "https://templates.example.invalid");
        app.UseKestrel(options => options.ListenLocalhost(5199));
        using var bootstrap = app.CreateClient();
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new System.Net.CookieContainer() };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5199") };
        JsonElement cookies;
        if (reuseSession)
        {
            // Reuse the disposable test session across a real process restart; do not reset Identity's password.
            using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(fixturePath));
            cookies = fixture.RootElement.GetProperty("cookies").Clone();
            foreach (var cookie in cookies.EnumerateArray())
                handler.CookieContainer.Add(new System.Net.Cookie(cookie.GetProperty("name").GetString()!, cookie.GetProperty("value").GetString()!, "/", "localhost"));
            using var check = await client.GetAsync("/dashboard/content/shared-projects");
            if (check.StatusCode != System.Net.HttpStatusCode.OK) throw new InvalidOperationException("Existing isolated test session is no longer valid.");
        }
        else
        {
            using var login = await AuthChecks.Login(client, AuthFactory.Email, app.Password);
            if (!login.Headers.TryGetValues("Set-Cookie", out var headers)) throw new InvalidOperationException("Test login failed: " + login.StatusCode);
            var cookieName = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme).Cookie.Name!;
            cookies = JsonSerializer.SerializeToElement(headers.Select(header => header.Split(';')[0].Split('=', 2))
                .Where(parts => parts[0].StartsWith(cookieName, StringComparison.Ordinal))
                .Select(parts => new { name = parts[0], value = parts[1], domain = "localhost", path = "/", httpOnly = true, secure = false, sameSite = "Lax" }).ToArray());
            if (cookies.GetArrayLength() == 0) throw new InvalidOperationException("Test login produced no Identity cookie.");
        }
        await using var scope = app.Services.CreateAsyncScope();
        var projects = await scope.ServiceProvider.GetRequiredService<IProjectCatalog>().GetAsync();
        var members = await scope.ServiceProvider.GetRequiredService<IMemberCatalog>().GetAsync();
        // Contains only disposable test-session cookies. Use an ignored local artifact path.
        await File.WriteAllTextAsync(fixturePath, JsonSerializer.Serialize(new { cookies, projects = projects.Select(x => "/" + x.DetailHref), members = members.Select(x => "/" + x.ProfileHref) }));
        Console.WriteLine("Detail-template browser host ready on localhost:5199 (isolated database; fake email only). No credentials printed.");
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}
