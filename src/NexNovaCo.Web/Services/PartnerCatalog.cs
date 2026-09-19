using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// One approved source shared by Home and About. No partner destination was supplied.
internal static class PartnerCatalog
{
    public static SectionHeading Heading { get; } = new("Our Partners",
        "We collaborate with industry-leading partners to bring cutting-edge technology and innovation to our clients.");
    public static IReadOnlyList<Partner> All { get; } = Array.AsReadOnly<Partner>([
        new("Tech Co", "Creative technology for modern businesses", "image/partnership/TechCo.png"),
        new("Digital Co", "AI-driven business automation", "image/partnership/digitalco.png"),
        new("NeTech Co", "Cutting-edge software solutions", "image/partnership/netechco.png", true),
        new("NeDigital Co", "Cloud-based software excellence", "image/partnership/nedigitalco.png"),
        new("Alpha Co", "Cutting-edge software Dev", "image/partnership/alphaco.png"),
        new("NeAlpha Co", "Cutting-edge software solutions", "image/partnership/nealphaco.png", true)
    ]);
}
