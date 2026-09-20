using System.Text.Json;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// One application-lifetime snapshot shared by Home, the listing and navigation-only detail stubs.
// Replace this DI implementation for a future content source, not the rendering components.
public sealed class ProjectCatalog(IWebHostEnvironment environment) : IProjectCatalog
{
    private readonly Lazy<Task<IReadOnlyList<ProjectSummary>>> _projects = new(() => LoadAsync(environment.WebRootPath));

    public Task<IReadOnlyList<ProjectSummary>> GetAsync(CancellationToken cancellationToken = default)
        => _projects.Value.WaitAsync(cancellationToken);

    private static async Task<IReadOnlyList<ProjectSummary>> LoadAsync(string webRoot)
    {
        await using var stream = File.OpenRead(Path.Combine(webRoot, "data", "projects.json"));
        var projects = await JsonSerializer.DeserializeAsync<ProjectJson[]>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("The canonical projects.json content was empty.");
        // JSON order matches approved project.html. Listing covers intentionally differ from
        // detail galleries (notably NexConnect); gallery/detail fields remain deferred.
        return Array.AsReadOnly(projects.Select(project => new ProjectSummary(
            project.Id, project.Name, project.SecondName, project.Subtitle, $"image/project/{project.Id}.jpg")).ToArray());
    }

    private sealed record ProjectJson(string Id, string Name, string SecondName, string Subtitle);
}
