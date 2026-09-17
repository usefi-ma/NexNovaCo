using System.Text.Json;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Temporary, read-only content provider. Canonical entity values come from the approved JSON;
/// editorial Home copy and featured ordering come from index.html. Replace this DI implementation
/// later, not the rendering components. Content is snapshotted once per application lifetime.
/// </summary>
public sealed class HomeContentService(IWebHostEnvironment environment) : IHomeContentService
{
    private readonly Lazy<Task<HomeContent>> _content = new(() => LoadAsync(environment.WebRootPath));

    public Task<HomeContent> GetAsync(CancellationToken cancellationToken = default)
        => _content.Value.WaitAsync(cancellationToken);

    private static async Task<HomeContent> LoadAsync(string webRoot)
    {
        var projects = await ReadAsync<ProjectJson>(webRoot, "projects.json");
        var members = await ReadAsync<MemberJson>(webRoot, "member.json");
        // Listing covers intentionally differ from detail galleries (notably NexConnect).
        var featuredProjects = new[] { "nexconnect", "payflowx", "medilink", "tradesync", "eduvance" }
            .Select(id => {
                var project = projects.Single(p => p.Id == id);
                return new ProjectSummary(project.Id, project.Name, project.SecondName, project.Subtitle,
                    $"image/project/{project.Id}.jpg");
            }).ToArray();
        // These short editorial teasers are not duplicate roles/bios; entity identity stays in JSON.
        var featuredMembers = new (string Id, string Introduction)[] {
            ("emilyjohnson", "Passionate about building scalable and efficient software solutions."),
            ("emmawilliams", "Leverages data-driven insights and innovative strategies."),
            ("sophialee", "Crafts user-focused interfaces with sleek, modern design principles."),
            ("danielkim", "Streamlines deployments and enhances system reliability at scale.")
        }.Select(feature => {
            var member = members.Single(m => m.Id == feature.Id);
            return new TeamMemberSummary(member.Id, member.Name, member.Role, feature.Introduction,
                member.Image.StartsWith("assets/", StringComparison.Ordinal) ? member.Image[7..] : member.Image,
                member.Email, member.LinkedIn, member.Telegram);
        }).ToArray();

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
            [new("software", "Software Dev", "Tailored Solutions", "We design software solutions that are perfectly aligned with your business needs.", "image/service/icons/online-services.png"),
             new("web-mobile", "Web & Mobile App", "High-Performance Applications", "We develop responsive, high-performance web and mobile apps for seamless user experiences.", "image/service/icons/software.png"),
             new("ai", "AI Solutions", "Intelligent Automation", "We integrate AI to automate processes and improve business efficiency.", "image/service/icons/analysing.png"),
             new("consulting", "Tech Consulting", "Future-Ready Solutions", "We provide expert tech consulting to help businesses adopt innovative technologies.", "image/service/icons/process.png"),
             new("design", "UX/UI Design", "User-Centric Designs", "We design intuitive, user-friendly interfaces for seamless and enjoyable experiences.", "image/service/icons/custom.png")],
            new("Our Projects", "We turn ideas into powerful digital solutions. Our projects reflect innovation, precision, and a commitment to excellence. From AI-driven applications to custom software and high-performance web and mobile solutions, we deliver cutting-edge technology that helps businesses grow. Explore our work and see how we bring visions to life with creativity and expertise."),
            featuredProjects,
            new("Our Team", "We bring together a team of skilled developers, creative designers, and tech strategists, all driven by a passion for innovation. Our diverse expertise allows us to build cutting-edge solutions that help businesses thrive in the digital era.",
                "From concept to execution, we prioritize innovation, efficiency, and user experience.",
                "With years of experience in custom software development, AI-powered applications, and web & mobile solutions, our team works collaboratively to transform ideas into reality.", "Learn more", "team"),
            featuredMembers,
            [new("PROJECTS", 450), new("CLIENTS", 3000), new("EMPLOYEES", 1000), new("AWARDS", 26)],
            new("Our Partners", "We collaborate with industry-leading partners to bring cutting-edge technology and innovation to our clients."),
            [new("Tech Co", "Creative technology for modern businesses", "image/partnership/TechCo.png"),
             new("Digital Co", "AI-driven business automation", "image/partnership/digitalco.png"),
             new("NeTech Co", "Cutting-edge software solutions", "image/partnership/netechco.png", true),
             new("NeDigital Co", "Cloud-based software excellence", "image/partnership/nedigitalco.png"),
             new("Alpha Co", "Cutting-edge software Dev", "image/partnership/alphaco.png"),
             new("NeAlpha Co", "Cutting-edge software solutions", "image/partnership/nealphaco.png", true)],
            [new("Olivia Carter, COO at Alpha Co",
                ["Partnering with NexNovaCo was a game-changer for our business. Their AI-driven web and mobile solutions streamlined our operations and gave us a competitive edge. The team is professional, responsive, and truly innovative — we saw measurable growth within just months of implementation.",
                 "We were particularly impressed by their attention to detail and ability to translate our complex requirements into user-friendly, scalable software. NexNovaCo didn't just deliver a solution — they delivered real value."]),
             new("Daniel Kim, Marketing Director at Tech Co",
                ["Working with NexNovaCo transformed our digital presence. Their AI-based analytics tools gave us deep insights into customer behavior, helping us improve engagement and retention dramatically. The process was smooth and collaborative from start to finish.",
                 "What stood out most was their commitment to quality and their genuine passion for innovation. NexNovaCo became more than a vendor — they became a strategic partner in our growth."])],
            new("NexNovaCo", "NexNovaCo delivers innovative AI-driven, web, and mobile solutions, empowering businesses with cutting-edge technology for growth and success."));
    }

    private static async Task<T[]> ReadAsync<T>(string root, string name)
    {
        await using var stream = File.OpenRead(Path.Combine(root, "data", name));
        return await JsonSerializer.DeserializeAsync<T[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException($"The canonical content file {name} was empty.");
    }

    // Narrow JSON projections; detail-page data is intentionally not modeled in this phase.
    private sealed record ProjectJson(string Id, string Name, string SecondName, string Subtitle);
    private sealed record MemberJson(string Id, string Name, string Role, string Image,
        string? Email, string? LinkedIn, string? Telegram);
}
