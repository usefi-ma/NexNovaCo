using System.ComponentModel.DataAnnotations;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

internal static class MediaAvailability
{
    // Optional for legacy non-upload service callers; generated paths fail closed without storage.
    public static void Require(IMediaStorageService? media, string path, MediaKind kind)
    {
        if (media is not null) media.RequireAvailable(path, kind);
        else if (!MediaPolicy.Bundled(kind).Contains(path, StringComparer.Ordinal))
            throw new ValidationException("Media storage is unavailable.");
    }
    public static string Resolve(IMediaStorageService? media, string path, MediaKind kind) =>
        media?.ResolvePublicPath(path, kind) ?? (MediaPolicy.Bundled(kind).Contains(path, StringComparer.Ordinal) ? path : MediaPolicy.Fallback(kind));
}
