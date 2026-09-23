using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ProjectEntity
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public string FullDescription { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string? Client { get; set; }
    public string? Category { get; set; }
    public string? Date { get; set; }
    public string? Technologies { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<ProjectGalleryImage> Gallery { get; set; } = [];
    public List<ProjectFeature> Features { get; set; } = [];
    public HomeFeaturedProject? HomeFeatured { get; set; }
    public ProjectSummary ToContent() => new(Slug, Name, Tagline, Description, ImagePath);
    public ProjectDetail ToDetail() => new(ToContent(), FullDescription,
        Gallery.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => new ProjectImage(x.Source, x.Alt)).ToArray(),
        new(Client, Category, Date, Technologies),
        Features.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => x.Text).ToArray());
    public ProjectEditModel ToEditModel() => ProjectEditModel.FromContent(ToDetail());
    public void SetContent(ProjectEditModel model)
    {
        Slug = ProjectSlugs.Normalize(model.Slug); Name = model.Name.Trim(); Tagline = model.Tagline.Trim();
        Description = model.Description.Trim(); FullDescription = model.FullDescription.Trim(); ImagePath = model.ImagePath;
        Client = model.Client?.Trim(); Category = model.Category?.Trim(); Date = model.Date?.Trim(); Technologies = model.Technologies?.Trim();
        Gallery.Clear();
        Gallery.AddRange(model.Gallery.Select((x, i) => new ProjectGalleryImage { Source = x.Source, Alt = x.Alt.Trim(), DisplayOrder = i + 1 }));
        Features.Clear();
        Features.AddRange(model.Features.Select((x, i) => new ProjectFeature { Text = x.Text.Trim(), DisplayOrder = i + 1 }));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
public sealed class ProjectGalleryImage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int DisplayOrder { get; set; }
    public string Source { get; set; } = "";
    public string Alt { get; set; } = "";
}
public sealed class ProjectFeature
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int DisplayOrder { get; set; }
    public string Text { get; set; } = "";
}
public sealed class HomeFeaturedProject
{
    public int ProjectId { get; set; }
    public int DisplayOrder { get; set; }
    public ProjectEntity Project { get; set; } = null!;
}
public sealed class ProjectInitializationState { public int Id { get; set; } = 1; }
