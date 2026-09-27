using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ProjectDetailTemplateSettings
{
    public int Id { get; set; } = 1;
    public string HeroCtaText { get; set; } = "";
    public string BreadcrumbHomeLabel { get; set; } = "";
    public string BreadcrumbSectionLabel { get; set; } = "";
    public string ProjectDetailsHeading { get; set; } = "";
    public string FeaturesHeading { get; set; } = "";
    public string ClientLabel { get; set; } = "";
    public string CategoryLabel { get; set; } = "";
    public string DateLabel { get; set; } = "";
    public string TechnologiesLabel { get; set; } = "";
    public string ReturnCtaText { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ProjectDetailTemplateEditModel ToEditModel() => new()
    {
        HeroCtaText = HeroCtaText,
        BreadcrumbHomeLabel = BreadcrumbHomeLabel,
        BreadcrumbSectionLabel = BreadcrumbSectionLabel,
        ProjectDetailsHeading = ProjectDetailsHeading,
        FeaturesHeading = FeaturesHeading,
        ClientLabel = ClientLabel,
        CategoryLabel = CategoryLabel,
        DateLabel = DateLabel,
        TechnologiesLabel = TechnologiesLabel,
        ReturnCtaText = ReturnCtaText,
    };
    public void SetContent(ProjectDetailTemplateEditModel model)
    {
        HeroCtaText = model.HeroCtaText.Trim();
        BreadcrumbHomeLabel = model.BreadcrumbHomeLabel.Trim();
        BreadcrumbSectionLabel = model.BreadcrumbSectionLabel.Trim();
        ProjectDetailsHeading = model.ProjectDetailsHeading.Trim();
        FeaturesHeading = model.FeaturesHeading.Trim();
        ClientLabel = model.ClientLabel.Trim();
        CategoryLabel = model.CategoryLabel.Trim();
        DateLabel = model.DateLabel.Trim();
        TechnologiesLabel = model.TechnologiesLabel.Trim();
        ReturnCtaText = model.ReturnCtaText.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class MemberDetailTemplateSettings
{
    public int Id { get; set; } = 1;
    public string BreadcrumbHomeLabel { get; set; } = "";
    public string BreadcrumbSectionLabel { get; set; } = "";
    public string SkillsHeading { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public MemberDetailTemplateEditModel ToEditModel() => new()
    {
        BreadcrumbHomeLabel = BreadcrumbHomeLabel,
        BreadcrumbSectionLabel = BreadcrumbSectionLabel,
        SkillsHeading = SkillsHeading,
    };
    public void SetContent(MemberDetailTemplateEditModel model)
    {
        BreadcrumbHomeLabel = model.BreadcrumbHomeLabel.Trim();
        BreadcrumbSectionLabel = model.BreadcrumbSectionLabel.Trim();
        SkillsHeading = model.SkillsHeading.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
