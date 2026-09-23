using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class HomeHeroSettings
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string OpeningLine { get; set; } = "";
    public string EmphasisLine { get; set; } = "";
    public string ClosingLine { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = MediaPolicy.HeroDefault;
    public DateTime UpdatedAtUtc { get; set; }

    public HomeHeroContent ToContent() => new(OpeningLine, EmphasisLine, ClosingLine, Description, CtaLabel, CtaHref, ImagePath);

    public void SetContent(HomeHeroContent content)
    {
        OpeningLine = content.OpeningLine;
        EmphasisLine = content.EmphasisLine;
        ClosingLine = content.ClosingLine;
        Description = content.Description;
        CtaLabel = content.CtaLabel;
        CtaHref = content.CtaHref;
        ImagePath = content.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
