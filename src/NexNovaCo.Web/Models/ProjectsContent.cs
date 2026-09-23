namespace NexNovaCo.Web.Models;

public sealed record ProjectsContent(InnerPageHeroContent Hero, IReadOnlyList<ProjectSummary> Projects,
    IReadOnlyList<Testimonial> Testimonials, SectionHeading TestimonialBrand, string HeroImagePath = MediaPolicy.ProjectsHeroDefault);
