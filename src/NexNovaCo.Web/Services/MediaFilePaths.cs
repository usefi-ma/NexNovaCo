using Microsoft.Extensions.Options;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Shared by storage and the read-only HTTP handler. No arbitrary filesystem paths from clients.
public sealed class MediaFilePaths(IWebHostEnvironment environment, IOptions<MediaStorageOptions> options)
{
    public string Root { get; } = Path.GetFullPath(options.Value.RootPath ?? Path.Combine(environment.WebRootPath, "uploads"));
    public string PhysicalPath(string publicPath)
    {
        if (!MediaPolicy.IsGenerated(publicPath)) throw new ArgumentException("Invalid upload path.");
        var path = Path.GetFullPath(Path.Combine(Root, publicPath["uploads/".Length..]));
        if (!path.StartsWith(Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid upload path.");
        // Do not follow existing junctions/symlinks, including an upload-root ancestor.
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked upload directories/files are not supported.");
        return path;
    }
    public bool Exists(string publicPath)
    {
        try { return File.Exists(PhysicalPath(publicPath)); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }
}
