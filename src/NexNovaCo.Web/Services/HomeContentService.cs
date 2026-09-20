using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Temporary, read-only content provider. Canonical entity values come from the approved JSON;
/// editorial Home copy and featured ordering come from index.html. Replace this DI implementation
/// later, not the rendering components. Content is snapshotted once per application lifetime.
/// </summary>
public sealed class HomeContentService(IProjectCatalog projectCatalog, IMemberCatalog memberCatalog) : IHomeContentService
{
    private readonly Lazy<Task<HomeContent>> _content = new(() => LoadAsync(projectCatalog, memberCatalog));

    public Task<HomeContent> GetAsync(CancellationToken cancellationToken = default)
        => _content.Value.WaitAsync(cancellationToken);

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
            new("Smart Software", "Powerful AI", "Endless Innovation",
                "We build high-performance web, mobile, and AI-driven applications to help businesses grow.",
                "Discover Our Services", "services"),
            new("Welcome to NexNovaCo",
                "Whether you're a startup bringing a bold new idea to life or an enterprise looking to enhance your digital presence, our team is committed to delivering tailored solutions that align with your unique needs.",
                ["From intuitive user experiences to powerful backend systems, we build software that is not only functional but also optimized for performance, security, and growth.",
                 "Our mission is to empower businesses with smart, scalable, and future-ready digital solutions. From AI-driven automation to tailored app development, we help our clients stay ahead in an ever-evolving digital world. Let's build the future together!"],
                "Learn more", "about"),
            new("Our Services", "We specialize in delivering custom software solutions, web and mobile app development, and AI-powered innovations that are tailored to help businesses achieve their goals. We work closely with our clients to understand their unique needs, delivering digital products that enhance efficiency, drive growth, and provide a competitive edge in today's fast-paced market."),
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
