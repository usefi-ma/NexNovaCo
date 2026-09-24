using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class PublicQualityChecks
{
    private const string Origin = "https://public.example";
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword(), "Production", publicBaseUrl: Origin);
        using var client = app.NewClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        string projectSlug, memberSlug;
        await using (var db = await factory.CreateDbContextAsync())
        {
            projectSlug = (await db.Projects.FirstAsync()).Slug;
            memberSlug = (await db.Members.FirstAsync()).Slug;
        }
        var paths = PublicSiteUrls.StaticPaths.Concat(new[] { "/projects/" + projectSlug, "/team/" + memberSlug }).ToArray();
        var titles = new HashSet<string>();
        foreach (var path in paths)
        {
            var response = await client.GetAsync(path + "?utm_source=quality");
            Check(response.IsSuccessStatusCode, "Public route renders: " + path);
            Check(!response.Headers.Contains("X-Robots-Tag"), "Production public route remains indexable.");
            var html = await response.Content.ReadAsStringAsync();
            Check(Regex.Matches(html, "<title>").Count == 1, "One title: " + path);
            Check(titles.Add(Regex.Match(html, "<title>(.*?)</title>").Groups[1].Value), "Unique title.");
            Check(Regex.Matches(html, "name=\"description\"").Count == 1, "One description.");
            Check(Regex.Matches(html, "rel=\"canonical\"").Count == 1 && html.Contains($"href=\"{Origin}{path}\""), "One canonical without query.");
            foreach (var property in new[] { "title", "description", "type", "url", "image", "site_name" })
                Check(html.Contains($"property=\"og:{property}\""), "OG " + property);
            Check(html.Contains("name=\"twitter:card\" content=\"summary\""), "Honest summary card.");
            var shareUrl = WebUtility.HtmlDecode(Regex.Match(html, "property=\"og:image\" content=\"([^\"]+)\"").Groups[1].Value);
            Check(Uri.TryCreate(shareUrl, UriKind.Absolute, out var shareUri) && shareUri.Host == "public.example", "Absolute, same-origin sharing image.");
            Check((await client.GetAsync(shareUri!.AbsolutePath)).IsSuccessStatusCode, "Sharing image resolves.");
            var json = Regex.Match(html, "<script type=\"application/ld(?:\\+|&#x2B;)json\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
            using var data = JsonDocument.Parse(json);
            Check(data.RootElement.GetProperty("@graph").GetArrayLength() == (path.Count(x => x == '/') == 2 ? 3 : 2), "Accurate schema graph/breadcrumbs.");
            Check(Regex.Matches(html, "<main[ >]").Count == 1 && Regex.Matches(html, "<h1[ >]").Count == 1, "One main/H1.");
            Check(html.Contains("class=\"skip-link\"") && html.Contains("id=\"main-content\" tabindex=\"-1\""), "Skip destination.");
            Check(!Regex.IsMatch(html, "<script[^>]+src=\"[^\"]*jquery-3\\.1\\.0"), "Old jQuery not loaded.");
        }
        foreach (var path in new[] { "/projects/not-a-real-project", "/team/not-a-real-member", "/not-found", "/missing-route" })
        {
            var response = await client.GetAsync(path);
            Check(response.StatusCode == HttpStatusCode.NotFound, "Real 404: " + path);
            Check(response.Headers.Contains("X-Robots-Tag"), "404 header noindex.");
            Check(!(await response.Content.ReadAsStringAsync()).Contains("rel=\"canonical\""), "Missing route has no canonical.");
        }
        foreach (var path in new[] { "/admin/login", "/admin/access-denied", "/dashboard", "/dashboard/settings/site", "/Error" })
        {
            var response = await client.GetAsync(path);
            Check(response.Headers.Contains("X-Robots-Tag"), "Private/error noindex header: " + path);
        }
        var robots = await client.GetStringAsync("/robots.txt");
        Check(robots.Contains("Allow: /") && !robots.Contains("Disallow: /\n") && robots.Contains(Origin + "/sitemap.xml"), "Production robots public + absolute sitemap.");
        var sitemap = await Sitemap(client);
        foreach (var path in paths) Check(sitemap.Contains(Origin + path), "Sitemap includes " + path);
        Check(sitemap.All(x => !x.Contains("dashboard") && !x.Contains("admin") && !x.Contains("not-found")), "Private/utility excluded.");
        var contact = await client.GetStringAsync("/contact");
        foreach (var field in new[] { "firstName", "lastName", "email", "subject", "message" })
            Check(contact.Contains($"for=\"{field}\"") && contact.Contains($"aria-describedby=\"{field}-error\""), "Associated Contact label/error: " + field);
        Check(contact.Contains("Subject is optional.") && contact.Contains("does not send or store messages"), "Visible required/demo instructions.");
        Check(contact.Contains("Newsletter signup is not available yet."), "Honest newsletter.");
        var services = await client.GetStringAsync("/services");
        Check(services.Contains("aria-expanded=\"false\"") && services.Contains("aria-controls=\"faq-answer-"), "FAQ semantics.");

        // The fixture is disposable. Simulate CMS content changes without touching the developer DB.
        await using (var db = await factory.CreateDbContextAsync())
        {
            var identity = await db.SiteIdentitySettings.SingleAsync();
            identity.SiteName = "Quality & Brand";
            var project = await db.Projects.FirstAsync(x => x.Slug == projectSlug);
            project.Name = "Quality Project"; project.Description = "A changed project description.";
            var member = await db.Members.FirstAsync(x => x.Slug == memberSlug);
            member.Name = "Quality Member"; member.Role = "Quality Engineer";
            member.Introduction = "A changed member introduction.";
            await db.SaveChangesAsync();
        }
        var projectHtml = await client.GetStringAsync("/projects/" + projectSlug);
        Check(projectHtml.Contains("Quality Project | Quality &amp; Brand") && projectHtml.Contains("A changed project description."), "Project/brand metadata follows content.");
        var memberHtml = await client.GetStringAsync("/team/" + memberSlug);
        Check(memberHtml.Contains("Quality Member | Quality &amp; Brand") && memberHtml.Contains("Quality Engineer"), "Member metadata follows content.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Projects.Remove(await db.Projects.SingleAsync(x => x.Slug == projectSlug));
            db.Members.Remove(await db.Members.SingleAsync(x => x.Slug == memberSlug));
            db.NavigationItems.RemoveRange(await db.NavigationItems.ToListAsync());
            await db.SaveChangesAsync();
        }
        var after = await Sitemap(client);
        Check(!after.Contains(Origin + "/projects/" + projectSlug) && !after.Contains(Origin + "/team/" + memberSlug), "Deleted details leave sitemap.");
        Check(PublicSiteUrls.StaticPaths.All(x => after.Contains(Origin + x)), "Empty navigation does not change canonical inventory.");
        using var authenticated = app.NewClient();
        Check((await Login(authenticated, AuthFactory.Email, app.Password)).StatusCode == HttpStatusCode.Redirect, "Admin login smoke.");
        Check((await authenticated.GetAsync("/dashboard")).IsSuccessStatusCode, "Dashboard load smoke.");
        foreach (var value in new[] { "http://public.example", "https://localhost", "https://public.example/subpath", "https://user:password@public.example", "https://public.example/?q=1" })
        {
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["PublicSite:BaseUrl"] = value }).Build();
            var rejected = false;
            try { _ = new PublicSiteUrls(configuration, app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<PublicSiteUrls>.Instance); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Unsafe origin rejected: " + value);
        }
        await using (var restarted = new AuthFactory(app.Password, "Production", app.DatabasePath, Origin))
        using (var restartClient = restarted.NewClient())
            Check(!(await Sitemap(restartClient)).Contains(Origin + "/projects/" + projectSlug), "Deletion persists on restart.");

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Projects RENAME TO QualityHiddenProjects");
            try
            {
                var unavailable = await client.GetAsync("/sitemap.xml");
                Check(unavailable.StatusCode == HttpStatusCode.ServiceUnavailable, "Database failure returns 503, never static fallback URLs.");
                Check(!(await unavailable.Content.ReadAsStringAsync()).Contains(projectSlug), "Failure exposes no deleted fallback URL.");
            }
            finally { await db.Database.ExecuteSqlRawAsync("ALTER TABLE QualityHiddenProjects RENAME TO Projects"); }
        }

        await using var development = new AuthFactory(NewPassword(), publicBaseUrl: Origin);
        using var devClient = development.NewClient();
        Check((await devClient.GetAsync("/")).Headers.Contains("X-Robots-Tag"), "Development noindex.");
        Check((await devClient.GetStringAsync("/robots.txt")).Contains("Disallow: /\n"), "Development robots.");
        await using var unset = new AuthFactory(NewPassword(), "Production", publicBaseUrl: "");
        using var unsetClient = unset.NewClient();
        Check((await unsetClient.GetAsync("/sitemap.xml")).StatusCode == HttpStatusCode.ServiceUnavailable, "No invented production domain.");
        Check((await unsetClient.GetAsync("/")).Headers.Contains("X-Robots-Tag"), "Unset production origin fails closed.");
    }

    private static async Task<HashSet<string>> Sitemap(HttpClient client)
    {
        var response = await client.GetAsync("/sitemap.xml");
        Check(response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/xml", "Sitemap XML content type.");
        var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        Check(xml.Root?.Name == ns + "urlset", "Sitemap namespace.");
        var locations = xml.Descendants(ns + "loc").Select(x => x.Value).ToList();
        Check(locations.Distinct().Count() == locations.Count, "No duplicate sitemap entries.");
        return locations.ToHashSet();
    }
}
