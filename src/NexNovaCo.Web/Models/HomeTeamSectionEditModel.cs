using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class HomeTeamSectionEditModel
{
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [Required, StringLength(500)] public string Introduction { get; set; } = "";
    [Required, StringLength(300), Display(Name = "Highlight")] public string Highlight { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Description")] public string Description { get; set; } = "";
    [Required, StringLength(60), Display(Name = "CTA text")] public string CtaLabel { get; set; } = "";
    // Identical allowlist to Hero: local public routes only, no schemes, encodings or admin paths.
    [Required, StringLength(200), Display(Name = "CTA route")]
    [RegularExpression(@"^(?:/|/?(?:about|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))$",
        ErrorMessage = "Use a public site route, such as /services, /projects, /projects/nexconnect or /.")]
    public string CtaHref { get; set; } = "";

    public static HomeTeamSectionEditModel FromContent(TeamSectionContent content) => new()
    {
        Title = content.Title, Introduction = content.Introduction,
        Highlight = content.Highlight, Description = content.Description,
        CtaLabel = content.CtaLabel, CtaHref = content.CtaHref
    };

    public TeamSectionContent ToContent() => new(Title, Introduction, Highlight, Description, CtaLabel, CtaHref);
}
