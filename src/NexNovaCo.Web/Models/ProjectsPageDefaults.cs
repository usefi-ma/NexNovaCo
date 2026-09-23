namespace NexNovaCo.Web.Models;

public static class ProjectsPageDefaults
{
    public static ProjectsContent Content { get; } = new(
        new("Our Projects", "Our Projects",
            "From AI-powered platforms to custom enterprise web solutions, we specialize in bringing digital ideas to life. Each project reflects our focus on innovation, user experience, and scalable performance.",
            "Explore Our Works", "projects#Project"),
        [], [], TestimonialPresentationDefaults.Brand);
}
