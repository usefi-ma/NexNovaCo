using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class TeamHeroSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public TeamHeroEditModel ToEditModel() => new() { Title = Title, Description = Description, CtaLabel = CtaLabel, CtaHref = CtaHref, ImagePath = ImagePath };
    public void SetContent(TeamHeroEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        CtaLabel = model.CtaLabel;
        CtaHref = model.CtaHref;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class TeamSectionSettings
{
    public int Id { get; set; } = 1;
    public string Eyebrow { get; set; } = "";
    public string Title { get; set; } = "";
    public string Introduction { get; set; } = "";
    public string Highlight { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public TeamSectionEditModel ToEditModel() => new() { Eyebrow = Eyebrow, Title = Title, Introduction = Introduction, Highlight = Highlight, Description = Description, CtaLabel = CtaLabel, CtaHref = CtaHref, ImagePath = ImagePath };
    public void SetContent(TeamSectionEditModel model)
    {
        Eyebrow = model.Eyebrow;
        Title = model.Title;
        Introduction = model.Introduction;
        Highlight = model.Highlight;
        Description = model.Description;
        CtaLabel = model.CtaLabel;
        CtaHref = model.CtaHref;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
