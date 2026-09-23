using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class TeamHeroEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(600)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), TeamPageCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.TeamHero)]
    public string ImagePath { get; set; } = "";
    public static TeamHeroEditModel Approved()
    {
        var c = TeamPageDefaults.Content.Hero;
        return new() { Title = c.Title, Description = c.Description!, CtaLabel = c.CtaLabel, CtaHref = c.CtaHref, ImagePath = MediaPolicy.TeamHeroDefault };
    }
}

public sealed class TeamSectionEditModel
{
    [Required, StringLength(80)]
    public string Eyebrow { get; set; } = "";
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(500)]
    public string Introduction { get; set; } = "";
    [Required, StringLength(300)]
    public string Highlight { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), TeamPageCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.TeamSection)]
    public string ImagePath { get; set; } = "";
    public static TeamSectionEditModel Approved()
    {
        var c = TeamPageDefaults.Content.Introduction;
        return new() { Eyebrow = TeamPageDefaults.Content.Eyebrow, Title = c.Title, Introduction = c.Introduction, Highlight = c.Highlight, Description = c.Description!, CtaLabel = c.CtaLabel, CtaHref = c.CtaHref, ImagePath = MediaPolicy.TeamSectionDefault };
    }
}

public sealed class TeamPageCtaRouteAttribute : ValidationAttribute
{
    public TeamPageCtaRouteAttribute() => ErrorMessage = "Use a public site route, or /team#Team for the Team section.";
    public override bool IsValid(object? value) => value is string path && Regex.IsMatch(path,
        @"\A(?:/|/?(?:about|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:#Team|/[a-z0-9]+(?:-[a-z0-9]+)*)?))\z", RegexOptions.CultureInvariant);
}
