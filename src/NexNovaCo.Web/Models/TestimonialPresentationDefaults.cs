namespace NexNovaCo.Web.Models;

// Page presentation, not testimonial entity data. Home and Projects initialize their own independent CMS settings.
public static class TestimonialPresentationDefaults
{
    public static SectionHeading Brand { get; } = new("NexNovaCo",
        "NexNovaCo delivers innovative AI-driven, web, and mobile solutions, empowering businesses with cutting-edge technology for growth and success.");
}
