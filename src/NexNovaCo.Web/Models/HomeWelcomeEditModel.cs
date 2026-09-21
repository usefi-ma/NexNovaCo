using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class HomeWelcomeEditModel
{
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [Required, StringLength(500)] public string Introduction { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Body paragraph 1")] public string ParagraphOne { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Body paragraph 2")] public string ParagraphTwo { get; set; } = "";
    [Required, StringLength(60), Display(Name = "CTA text")] public string CtaLabel { get; set; } = "";
    // Identical allowlist to Hero: local public routes only, no schemes, encodings or admin paths.
    [Required, StringLength(200), Display(Name = "CTA route")]
    [RegularExpression(@"^(?:/|/?(?:about|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))$",
        ErrorMessage = "Use a public site route, such as /services, /projects, /projects/nexconnect or /.")]
    public string CtaHref { get; set; } = "";

    public static HomeWelcomeEditModel FromContent(WelcomeContent content) => new()
    {
        Title = content.Title, Introduction = content.Introduction,
        ParagraphOne = content.Paragraphs[0], ParagraphTwo = content.Paragraphs[1],
        CtaLabel = content.CtaLabel, CtaHref = content.CtaHref
    };

    public WelcomeContent ToContent() => new(Title, Introduction, [ParagraphOne, ParagraphTwo], CtaLabel, CtaHref);
}
