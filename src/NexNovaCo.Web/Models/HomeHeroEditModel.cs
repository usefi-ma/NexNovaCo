using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class HomeHeroEditModel
{
    [Required, StringLength(60), Display(Name = "Opening line")] public string OpeningLine { get; set; } = "";
    [Required, StringLength(60), Display(Name = "Emphasis line")] public string EmphasisLine { get; set; } = "";
    [Required, StringLength(60), Display(Name = "Closing line")] public string ClosingLine { get; set; } = "";
    [Required, StringLength(500)] public string Description { get; set; } = "";
    [Required, StringLength(60), Display(Name = "CTA text")] public string CtaLabel { get; set; } = "";
    // Preserve the existing relative "services" link, also accepting root-relative public routes.
    // No external schemes, protocol-relative URLs, encodings, backslashes, queries or admin links.
    [Required, StringLength(200), Display(Name = "CTA route")]
    [RegularExpression(@"^(?:/|/?(?:about|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))$",
        ErrorMessage = "Use a public site route, such as /services, /projects, /projects/nexconnect or /.")]
    public string CtaHref { get; set; } = "";

    [Required, StringLength(200), HomeImagePath(MediaKind.Hero)]
    public string ImagePath { get; set; } = MediaPolicy.HeroDefault;

    public static HomeHeroEditModel FromContent(HomeHeroContent content) => new()
    {
        OpeningLine = content.OpeningLine, EmphasisLine = content.EmphasisLine,
        ClosingLine = content.ClosingLine, Description = content.Description,
        CtaLabel = content.CtaLabel, CtaHref = content.CtaHref, ImagePath = content.ImagePath
    };

    public HomeHeroContent ToContent() => new(OpeningLine, EmphasisLine, ClosingLine, Description, CtaLabel, CtaHref, ImagePath);
}
