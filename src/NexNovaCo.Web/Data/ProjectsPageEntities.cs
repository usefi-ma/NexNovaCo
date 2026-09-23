using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ProjectsHeroSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string MobileTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ProjectsHeroEditModel ToEditModel() => new() { Title = Title, MobileTitle = MobileTitle, Description = Description, CtaLabel = CtaLabel, CtaHref = CtaHref, ImagePath = ImagePath };
    public void SetContent(ProjectsHeroEditModel model)
    {
        Title = model.Title;
        MobileTitle = model.MobileTitle;
        Description = model.Description;
        CtaLabel = model.CtaLabel;
        CtaHref = model.CtaHref;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ProjectsTestimonialsSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ProjectsTestimonialsEditModel ToEditModel() => new() { Title = Title, Description = Description };
    public void SetContent(ProjectsTestimonialsEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
