using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Data;

namespace NexNovaCo.Web.Services;

public static partial class PublicDiscoveryEndpoints
{
    public static void MapPublicDiscovery(this WebApplication app)
    {
        app.MapGet("/robots.txt", (PublicSiteUrls urls) => urls.BaseUri is null
            ? Results.Text("Public origin is not configured.\n", "text/plain", statusCode: 503)
            : Results.Text(urls.IsIndexable
                ? $"User-agent: *\nAllow: /\nDisallow: /admin/\nDisallow: /dashboard\nSitemap: {urls.Absolute("/sitemap.xml")}\n"
                : "User-agent: *\nDisallow: /\n", "text/plain")).AllowAnonymous();

        app.MapGet("/sitemap.xml", async (PublicSiteUrls urls, IDbContextFactory<ApplicationDbContext> factory,
            ILoggerFactory logging, HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-cache";
            if (urls.BaseUri is null) return Results.Text("Public origin is not configured.", "text/plain", statusCode: 503);
            try
            {
                // Do not use public catalog fallback: a failed database must not resurrect deleted URLs.
                await using var db = await factory.CreateDbContextAsync(context.RequestAborted);
                var projects = await db.Projects.AsNoTracking().Select(x => new { x.Slug, x.UpdatedAtUtc }).ToListAsync(context.RequestAborted);
                var members = await db.Members.AsNoTracking().Select(x => new { x.Slug, x.UpdatedAtUtc }).ToListAsync(context.RequestAborted);
                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                XElement Entry(string path, DateTime? updated = null) => new(ns + "url",
                    new XElement(ns + "loc", urls.Absolute(path)),
                    updated is { } timestamp && timestamp > DateTime.UnixEpoch
                        ? new XElement(ns + "lastmod", timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)) : null);
                var xml = new XDocument(new XElement(ns + "urlset",
                    PublicSiteUrls.StaticPaths.Select(path => Entry(path)),
                    projects.Where(x => SlugPattern().IsMatch(x.Slug)).OrderBy(x => x.Slug).Select(x => Entry("/projects/" + x.Slug, x.UpdatedAtUtc)),
                    members.Where(x => SlugPattern().IsMatch(x.Slug)).OrderBy(x => x.Slug).Select(x => Entry("/team/" + x.Slug, x.UpdatedAtUtc))));
                return Results.Text(xml.ToString(), "application/xml");
            }
            catch (DbException exception)
            {
                logging.CreateLogger("PublicDiscovery").LogError(exception, "Could not read sitemap content; no fallback URLs were emitted.");
                return Results.Text("Sitemap temporarily unavailable.", "text/plain", statusCode: 503);
            }
        }).AllowAnonymous();
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
