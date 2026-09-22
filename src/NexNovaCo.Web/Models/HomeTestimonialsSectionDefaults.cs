using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Models;

public static class HomeTestimonialsSectionDefaults
{
    // Reuse the approved shared introduction for first initialization/fallback only.
    // Home edits never change TestimonialCatalog.Brand (still used by Projects) or its testimonial collection.
    public static SectionHeading Content => TestimonialCatalog.Brand;
}
