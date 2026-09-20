using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Approved team.html editorial composition; member identities come only from the catalog.
public sealed class TeamContentService(IMemberCatalog memberCatalog) : ITeamContentService
{
    private readonly Lazy<Task<TeamContent>> _content = new(() => LoadAsync(memberCatalog));
    public Task<TeamContent> GetAsync(CancellationToken cancellationToken = default)
        => _content.Value.WaitAsync(cancellationToken);

    private static async Task<TeamContent> LoadAsync(IMemberCatalog memberCatalog) => new(
        new("Our Team", "Our Team", "Our team is a dynamic blend of creative minds, tech innovators, and problem-solvers dedicated to bringing your vision to life. We collaborate, strategize, and build with passion to drive success for every project.",
            "Explore Our Team", "team#Team"),
        "Behind the Scenes",
        new("Meet Our Team", "We bring together a team of skilled developers, creative designers, and tech strategists, all driven by a passion for innovation. Our diverse expertise allows us to build cutting-edge solutions that help businesses thrive in the digital era.",
            "From concept to execution, we prioritize innovation, efficiency, and user experience.",
            "With years of experience in custom software development, AI-powered applications, and web & mobile solutions, our team works collaboratively to transform ideas into reality. We believe in precision, creativity, and delivering results that exceed expectations.",
            "Want to collaborate with us?", "contact"),
        await memberCatalog.GetAsync());
}
