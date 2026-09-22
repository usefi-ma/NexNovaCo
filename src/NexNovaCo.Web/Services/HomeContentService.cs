using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Hero, Welcome and the Services heading are CMS-backed. Other Home content retains its approved static sources.
/// Editable content is read on every request/navigation, never held in the static snapshot.
/// </summary>
public sealed class HomeContentService(IProjectCatalog projectCatalog, IMemberCatalog memberCatalog,
    IHomeHeroContentService heroContent, IHomeWelcomeContentService welcomeContent,
    IHomeServicesSectionContentService servicesContent) : IHomeContentService
{
    private readonly Lazy<Task<HomeContent>> _content = new(() => LoadAsync(projectCatalog, memberCatalog));

    public async Task<HomeContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var content = await _content.Value.WaitAsync(cancellationToken);
        return content with
        {
            Hero = await heroContent.GetAsync(cancellationToken),
            Welcome = await welcomeContent.GetAsync(cancellationToken),
            ServicesHeading = await servicesContent.GetAsync(cancellationToken)
        };
    }

    private static async Task<HomeContent> LoadAsync(IProjectCatalog projectCatalog, IMemberCatalog memberCatalog)
    {
        var projects = await projectCatalog.GetAsync();
        var members = await memberCatalog.GetAsync();
        // Home keeps its original five featured identities from the same catalog as the listing.
        var featuredProjects = new[] { "nexconnect", "payflowx", "medilink", "tradesync", "eduvance" }
            .Select(id => projects.Single(project => project.Slug == id)).ToArray();
        var featuredMembers = new[] { "emilyjohnson", "emmawilliams", "sophialee", "danielkim" }
            .Select(id => members.Single(member => member.Slug == id)).ToArray();

        return new HomeContent(
            HomeHeroDefaults.Content,
            HomeWelcomeDefaults.Content,
            HomeServicesSectionDefaults.Content,
            ServiceCatalog.HomeFeatured,
            new("Our Projects", "We turn ideas into powerful digital solutions. Our projects reflect innovation, precision, and a commitment to excellence. From AI-driven applications to custom software and high-performance web and mobile solutions, we deliver cutting-edge technology that helps businesses grow. Explore our work and see how we bring visions to life with creativity and expertise."),
            featuredProjects,
            new("Our Team", "We bring together a team of skilled developers, creative designers, and tech strategists, all driven by a passion for innovation. Our diverse expertise allows us to build cutting-edge solutions that help businesses thrive in the digital era.",
                "From concept to execution, we prioritize innovation, efficiency, and user experience.",
                "With years of experience in custom software development, AI-powered applications, and web & mobile solutions, our team works collaboratively to transform ideas into reality.", "Learn more", "team"),
            featuredMembers,
            [new("PROJECTS", 450), new("CLIENTS", 3000), new("EMPLOYEES", 1000), new("AWARDS", 26)],
            PartnerCatalog.Heading, PartnerCatalog.All,
            TestimonialCatalog.All, TestimonialCatalog.Brand);
    }

}
