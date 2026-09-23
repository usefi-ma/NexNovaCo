using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ProjectsHeroEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(60)]
    public string MobileTitle { get; set; } = "";
    [Required, StringLength(600)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), ProjectsPageCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.ProjectsHero)]
    public string ImagePath { get; set; } = "";
    public static ProjectsHeroEditModel Approved()
    {
        var c = ProjectsPageDefaults.Content.Hero;
        return new() { Title = c.Title, MobileTitle = c.MobileTitle, Description = c.Description!, CtaLabel = c.CtaLabel, CtaHref = c.CtaHref, ImagePath = MediaPolicy.ProjectsHeroDefault };
    }
}

public sealed class ProjectsTestimonialsEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    public static ProjectsTestimonialsEditModel Approved()
    {
        var c = ProjectsPageDefaults.Content.TestimonialBrand;
        return new() { Title = c.Title, Description = c.Description! };
    }
}

public sealed class ProjectsPageCtaRouteAttribute : ValidationAttribute
{
    public ProjectsPageCtaRouteAttribute() => ErrorMessage = "Use a public site route, or /projects#Project for the project cards.";
    public override bool IsValid(object? value) => value is string path && Regex.IsMatch(path,
        @"\A(?:/|/?(?:about|services|contact|projects(?:#Project|/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))\z", RegexOptions.CultureInvariant);
}
