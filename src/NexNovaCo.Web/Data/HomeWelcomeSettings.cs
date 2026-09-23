using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class HomeWelcomeSettings
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string Title { get; set; } = "";
    public string Introduction { get; set; } = "";
    public string ParagraphOne { get; set; } = "";
    public string ParagraphTwo { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = MediaPolicy.WelcomeDefault;
    public DateTime UpdatedAtUtc { get; set; }

    public WelcomeContent ToContent() => new(Title, Introduction, [ParagraphOne, ParagraphTwo], CtaLabel, CtaHref, ImagePath);

    public void SetContent(WelcomeContent content)
    {
        Title = content.Title;
        Introduction = content.Introduction;
        ParagraphOne = content.Paragraphs[0];
        ParagraphTwo = content.Paragraphs[1];
        CtaLabel = content.CtaLabel;
        CtaHref = content.CtaHref;
        ImagePath = content.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
