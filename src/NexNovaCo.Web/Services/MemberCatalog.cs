using System.Text.Json;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Approved defaults used only for one-time initialization and public read-failure fallback.
public sealed class MemberCatalog(IWebHostEnvironment environment)
{
    public static IReadOnlyList<string> HomeFeaturedSlugs { get; } = Array.AsReadOnly(new[] { "emilyjohnson", "emmawilliams", "sophialee", "danielkim" });
    public async Task<IReadOnlyList<MemberDetail>> GetDetailsAsync(CancellationToken cancellationToken = default)
        => (await _members.Value.WaitAsync(cancellationToken)).Details;
    public async Task<IReadOnlyList<TeamMemberSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default)
    {
        var members = await GetAsync(cancellationToken);
        return HomeFeaturedSlugs.Select(slug => members.Single(x => x.Slug == slug)).ToArray();
    }
    private readonly Lazy<Task<Snapshot>> _members = new(() => LoadAsync(environment.WebRootPath));

    public async Task<IReadOnlyList<TeamMemberSummary>> GetAsync(CancellationToken cancellationToken = default)
        => (await _members.Value.WaitAsync(cancellationToken)).Summaries;

    public async Task<MemberDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default)
        => (await _members.Value.WaitAsync(cancellationToken)).Details
            .FirstOrDefault(member => string.Equals(member.Summary.Slug, slug, StringComparison.Ordinal));

    private static async Task<Snapshot> LoadAsync(string webRoot)
    {
        await using var stream = File.OpenRead(Path.Combine(webRoot, "data", "member.json"));
        var members = await JsonSerializer.DeserializeAsync<MemberJson[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("The canonical member.json content was empty.");
        // Short editorial teasers from team.html (the first four also appear on Home).
        // They are not alternate biographies or duplicate identity/role/social records.
        var introductions = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["emilyjohnson"] = "Passionate about building scalable and efficient software solutions.",
            ["emmawilliams"] = "Leverages data-driven insights and innovative strategies.",
            ["sophialee"] = "Crafts user-focused interfaces with sleek, modern design principles.",
            ["danielkim"] = "Streamlines deployments and enhances system reliability at scale.",
            ["lenaalvarez"] = "Transforms raw data into actionable insights using ML techniques.",
            ["jamespark"] = "Ensures seamless coordination across teams and successful delivery."
        };
        // JSON order is the approved Team order. All consumers share the same summary objects.
        var details = members.Select(member => new MemberDetail(
            new(member.Id, member.Name, member.Role, introductions.GetValueOrDefault(member.Id, ""),
                member.Image.StartsWith("assets/", StringComparison.Ordinal) ? member.Image[7..] : member.Image,
                member.Email, member.LinkedIn, member.Telegram),
            member.Bio, Array.AsReadOnly(member.Skills))).ToArray();
        return new(Array.AsReadOnly(details.Select(member => member.Summary).ToArray()), Array.AsReadOnly(details));
    }

    private sealed record Snapshot(IReadOnlyList<TeamMemberSummary> Summaries, IReadOnlyList<MemberDetail> Details);
    private sealed record MemberJson(string Id, string Name, string Role, string Image, string Bio,
        string[] Skills, string? Email, string? LinkedIn, string? Telegram);
}
