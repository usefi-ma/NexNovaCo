using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class HomeTeamSectionSettings
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string Title { get; set; } = "";
    public string Introduction { get; set; } = "";
    public string Highlight { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }

    public TeamSectionContent ToContent() => new(Title, Introduction, Highlight, Description, CtaLabel, CtaHref);

    public void SetContent(TeamSectionContent content)
    {
        Title = content.Title;
        Introduction = content.Introduction;
        Highlight = content.Highlight;
        Description = content.Description;
        CtaLabel = content.CtaLabel;
        CtaHref = content.CtaHref;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
