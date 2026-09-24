namespace NexNovaCo.Web.Services;

// Deployment configuration, not CMS content or an untrusted request Host header.
public sealed class PublicSiteUrls
{
    public Uri? BaseUri { get; }
    public bool IsIndexable { get; }
    public static readonly string[] StaticPaths = ["/", "/about", "/services", "/projects", "/team", "/contact"];

    public PublicSiteUrls(IConfiguration configuration, IWebHostEnvironment environment, ILogger<PublicSiteUrls> logger)
    {
        var value = configuration["PublicSite:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                uri.IsLoopback || uri.HostNameType != UriHostNameType.Dns || !uri.IsDefaultPort ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/")
                throw new InvalidOperationException("PublicSite:BaseUrl must be the approved HTTPS public origin (no path, query, credentials or local host).");
            BaseUri = uri;
        }
        IsIndexable = environment.IsProduction() && BaseUri is not null;
        if (BaseUri is null) logger.LogWarning("PublicSite:BaseUrl is unset. Public pages are noindex and sitemap/robots return 503 until an approved public origin is configured.");
    }

    public string? Absolute(string path) => BaseUri is null ? null : new Uri(BaseUri, path.TrimStart('/')).AbsoluteUri;
}
