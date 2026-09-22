using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Approved one-time seed and read-failure fallback only. SQLite is the live collection.
internal static class PartnerCatalog
{
    public static IReadOnlyList<Partner> All { get; } = Array.AsReadOnly<Partner>([
        new("Tech Co", "Creative technology for modern businesses", "image/partnership/TechCo.png"),
        new("Digital Co", "AI-driven business automation", "image/partnership/digitalco.png"),
        new("NeTech Co", "Cutting-edge software solutions", "image/partnership/netechco.png", true),
        new("NeDigital Co", "Cloud-based software excellence", "image/partnership/nedigitalco.png"),
        new("Alpha Co", "Cutting-edge software Dev", "image/partnership/alphaco.png"),
        new("NeAlpha Co", "Cutting-edge software solutions", "image/partnership/nealphaco.png", true)
    ]);
}
