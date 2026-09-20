using System.Text.Json;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// One application-lifetime snapshot shared by Home, the listing and Project Detail.
// Replace this DI implementation for a future content source, not the rendering components.
public sealed class ProjectCatalog(IWebHostEnvironment environment) : IProjectCatalog
{
    private readonly Lazy<Task<Snapshot>> _projects = new(() => LoadAsync(environment.WebRootPath));

    public async Task<IReadOnlyList<ProjectSummary>> GetAsync(CancellationToken cancellationToken = default)
        => (await _projects.Value.WaitAsync(cancellationToken)).Summaries;

    public async Task<ProjectDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default)
        => (await _projects.Value.WaitAsync(cancellationToken)).Details
            .FirstOrDefault(project => string.Equals(project.Summary.Slug, slug, StringComparison.Ordinal));

    private static async Task<Snapshot> LoadAsync(string webRoot)
    {
        await using var stream = File.OpenRead(Path.Combine(webRoot, "data", "projects.json"));
        var projects = await JsonSerializer.DeserializeAsync<ProjectJson[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("The canonical projects.json content was empty.");
        // JSON order matches approved project.html. Both views share the very same summary
        // objects; the detail projection uses only additional fields present in that JSON.
        var details = projects.Select(project => new ProjectDetail(
            new(project.Id, project.Name, project.SecondName, project.Subtitle, $"image/project/{project.Id}.jpg"),
            project.Summary,
            Array.AsReadOnly(project.Gallery.Select(image => new ProjectImage(
                image.Src.StartsWith("assets/", StringComparison.Ordinal) ? image.Src[7..] : image.Src,
                string.IsNullOrWhiteSpace(image.Alt) ? $"{project.Name} image" : image.Alt)).ToArray()),
            project.Details, Array.AsReadOnly(project.Features))).ToArray();
        return new(Array.AsReadOnly(details.Select(project => project.Summary).ToArray()), Array.AsReadOnly(details));
    }

    private sealed record Snapshot(IReadOnlyList<ProjectSummary> Summaries, IReadOnlyList<ProjectDetail> Details);
    private sealed record ImageJson(string Src, string? Alt);
    private sealed record ProjectJson(string Id, string Name, string SecondName, string Subtitle,
        string Summary, ImageJson[] Gallery, string[] Features, ProjectMetadata Details);
}
