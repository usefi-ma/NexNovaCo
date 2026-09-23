using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class AboutHeroEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(60)]
    public string MobileTitle { get; set; } = "";
    [Required, StringLength(600)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), AboutCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.AboutHero)]
    public string ImagePath { get; set; } = "";

    public static AboutHeroEditModel Approved()
    {
        var c = AboutDefaults.Content.Hero;
        return new() { Title = c.Title, MobileTitle = c.MobileTitle, Description = c.Description, CtaLabel = c.CtaLabel, CtaHref = c.CtaHref, ImagePath = MediaPolicy.AboutHeroDefault };
    }
}

public sealed class AboutStoryEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(200)]
    public string Subtitle { get; set; } = "";
    [Required, StringLength(1000)]
    public string IntroductionOne { get; set; } = "";
    [Required, StringLength(1000)]
    public string IntroductionTwo { get; set; } = "";
    [Required, StringLength(1000)]
    public string DetailOne { get; set; } = "";
    [Required, StringLength(1000)]
    public string DetailTwo { get; set; } = "";
    [Required, StringLength(500)]
    public string Closing { get; set; } = "";

    public static AboutStoryEditModel Approved()
    {
        var c = AboutDefaults.Content.Story;
        return new() { Title = c.Title, Subtitle = c.Subtitle, IntroductionOne = c.Introduction[0], IntroductionTwo = c.Introduction[1], DetailOne = c.Detail[0], DetailTwo = c.Detail[1], Closing = c.Closing };
    }
}

public sealed class AboutVisionEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string ParagraphOne { get; set; } = "";
    [Required, StringLength(1000)]
    public string ParagraphTwo { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), AboutCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.AboutVision)]
    public string ImagePath { get; set; } = "";
    [Required, StringLength(200)]
    public string ImageAlt { get; set; } = "";

    public static AboutVisionEditModel Approved()
    {
        var c = AboutDefaults.Content.Vision;
        return new() { Title = c.Title, ParagraphOne = c.Paragraphs[0], ParagraphTwo = c.Paragraphs[1], CtaLabel = c.CtaLabel, CtaHref = c.CtaHref, ImagePath = c.ImagePath, ImageAlt = c.ImageAlt };
    }
}

public sealed class AboutTimelineEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), AboutCtaRoute]
    public string CtaHref { get; set; } = "";

    public static AboutTimelineEditModel Approved()
    {
        var c = AboutDefaults.Content.Timeline;
        return new() { Title = c.Heading.Title, Description = c.Heading.Description!, CtaLabel = c.CtaLabel, CtaHref = c.CtaHref };
    }
}

public sealed class AboutMissionEditModel
{
    [Required, StringLength(80)]
    public string BrandTitle { get; set; } = "";
    [Required, StringLength(500)]
    public string BrandDescription { get; set; } = "";
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string ParagraphOne { get; set; } = "";
    [Required, StringLength(1000)]
    public string ParagraphTwo { get; set; } = "";

    public static AboutMissionEditModel Approved()
    {
        var c = AboutDefaults.Content.Mission;
        return new() { BrandTitle = c.Brand.Title, BrandDescription = c.Brand.Description!, Title = c.Title, ParagraphOne = c.Paragraphs[0], ParagraphTwo = c.Paragraphs[1] };
    }
}

public sealed class AboutPartnersEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";

    public static AboutPartnersEditModel Approved()
    {
        var c = AboutDefaults.Content.PartnersHeading;
        return new() { Title = c.Title, Description = c.Description! };
    }
}

public sealed class AboutTimelineItemEditModel
{
    [Range(1000, 9999)] public int Year { get; set; } = DateTime.UtcNow.Year;
    [Required, StringLength(600)] public string Description { get; set; } = "";
}

public sealed class AboutMissionPointEditModel
{
    [Required, StringLength(300)] public string Description { get; set; } = "";
}

public sealed class AboutCtaRouteAttribute : ValidationAttribute
{
    public AboutCtaRouteAttribute() => ErrorMessage = "Use a public site route, or /about#About for the existing Story anchor.";
    public override bool IsValid(object? value) => value is string path && Regex.IsMatch(path,
        @"\A(?:/|/?(?:about(?:#About)?|services|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))\z", RegexOptions.CultureInvariant);
}
