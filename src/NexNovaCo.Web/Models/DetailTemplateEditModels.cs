using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ProjectDetailTemplateEditModel
{
    [Required, StringLength(60), DetailTemplateText]
    public string HeroCtaText { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string BreadcrumbHomeLabel { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string BreadcrumbSectionLabel { get; set; } = "";
    [Required, StringLength(120), DetailTemplateText]
    public string ProjectDetailsHeading { get; set; } = "";
    [Required, StringLength(120), DetailTemplateText]
    public string FeaturesHeading { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string ClientLabel { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string CategoryLabel { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string DateLabel { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string TechnologiesLabel { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string ReturnCtaText { get; set; } = "";
    public static ProjectDetailTemplateEditModel Approved() => new()
    {
        HeroCtaText = "Explore the project",
        BreadcrumbHomeLabel = "Home",
        BreadcrumbSectionLabel = "Projects",
        ProjectDetailsHeading = "Project Details",
        FeaturesHeading = "Features of the project",
        ClientLabel = "Client",
        CategoryLabel = "Category",
        DateLabel = "Date",
        TechnologiesLabel = "Technologies",
        ReturnCtaText = "View More Projects?",
    };
}

public sealed class MemberDetailTemplateEditModel
{
    [Required, StringLength(60), DetailTemplateText]
    public string BreadcrumbHomeLabel { get; set; } = "";
    [Required, StringLength(60), DetailTemplateText]
    public string BreadcrumbSectionLabel { get; set; } = "";
    [Required, StringLength(120), DetailTemplateText]
    public string SkillsHeading { get; set; } = "";
    public static MemberDetailTemplateEditModel Approved() => new()
    {
        BreadcrumbHomeLabel = "Home",
        BreadcrumbSectionLabel = "Team",
        SkillsHeading = "Skills",
    };
}

// Plain labels, not rich text. Ordinary punctuation/Unicode remain valid.
public sealed class DetailTemplateTextAttribute : ValidationAttribute
{
    public DetailTemplateTextAttribute() => ErrorMessage = "Use plain single-line text without HTML.";
    public override bool IsValid(object? value) => value is string text &&
        !text.Any(char.IsControl) && !Regex.IsMatch(text, @"<\s*(?:[A-Za-z/!?])[^>]*>", RegexOptions.CultureInvariant);
}
