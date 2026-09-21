namespace NexNovaCo.Web.Models;

// Approved pre-CMS copy. Also the public read fallback; never reapplied over saved content.
public static class HomeHeroDefaults
{
    public static HomeHeroContent Content { get; } = new(
        "Smart Software", "Powerful AI", "Endless Innovation",
        "We build high-performance web, mobile, and AI-driven applications to help businesses grow.",
        "Discover Our Services", "services");
}
