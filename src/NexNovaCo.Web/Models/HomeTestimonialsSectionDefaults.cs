namespace NexNovaCo.Web.Models;

public static class HomeTestimonialsSectionDefaults
{
    // Reuse the approved shared introduction for first initialization/fallback only.
    // Home edits never change Projects presentation or the separate shared testimonial collection.
    public static SectionHeading Content => TestimonialPresentationDefaults.Brand;
}
