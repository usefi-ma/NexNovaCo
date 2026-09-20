namespace NexNovaCo.Web.Models;

// Detail media is deliberately separate from Summary.ImagePath (the listing cover).
public sealed record ProjectImage(string Source, string Alt);
public sealed record ProjectMetadata(string? Client, string? Category, string? Date, string? Technologies);
public sealed record ProjectDetail(ProjectSummary Summary, string FullDescription,
    IReadOnlyList<ProjectImage> Gallery, ProjectMetadata Metadata, IReadOnlyList<string> Features);
