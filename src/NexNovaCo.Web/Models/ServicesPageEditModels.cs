using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ServicesHeroEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(60)]
    public string MobileTitle { get; set; } = "";
    [Required, StringLength(600)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), ServicesPageCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.ServicesHero)]
    public string ImagePath { get; set; } = "";
    public static ServicesHeroEditModel Approved()
    {
        var c = ServicesPageDefaults.Content.Hero;
        return new() { Title = c.Title, MobileTitle = c.MobileTitle, Description = c.Description!, CtaLabel = c.CtaLabel, CtaHref = c.CtaHref, ImagePath = MediaPolicy.ServicesHeroDefault };
    }
}

public sealed class ServicesBenefitsEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.ServicesBenefits)]
    public string ImagePath { get; set; } = "";
    public static ServicesBenefitsEditModel Approved()
    {
        var c = ServicesPageDefaults.Content.BenefitsHeading;
        return new() { Title = c.Title, Description = c.Description!, ImagePath = MediaPolicy.ServicesBenefitsDefault };
    }
}

public sealed class ServicesProcessEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(500)]
    public string Description { get; set; } = "";
    public static ServicesProcessEditModel Approved()
    {
        var c = ServicesPageDefaults.Content.ProcessHeading;
        return new() { Title = c.Title, Description = c.Description! };
    }
}

public sealed class ServicesPricingEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    public static ServicesPricingEditModel Approved()
    {
        var c = ServicesPageDefaults.Content.PricingHeading;
        return new() { Title = c.Title, Description = c.Description! };
    }
}

public sealed class ServicesFaqEditModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    public static ServicesFaqEditModel Approved()
    {
        var c = ServicesPageDefaults.Content.FaqHeading;
        return new() { Title = c.Title, Description = c.Description! };
    }
}

public sealed class ServiceBenefitEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(600)]
    public string Description { get; set; } = "";
}

public sealed class ServiceProcessEditModel
{
    [Required, StringLength(60)]
    public string Title { get; set; } = "";
    [EnumDataType(typeof(ProcessIcon))]
    public ProcessIcon Icon { get; set; }
}

public sealed class ServicePricingEditModel : IValidatableObject
{
    [Required, StringLength(60)]
    public string Name { get; set; } = "";
    [Required, StringLength(100)]
    public string Subtitle { get; set; } = "";
    [Required, StringLength(40), RegularExpression(@"\A[A-Z]{3} (?:0|[1-9][0-9]*|[1-9][0-9]{0,2}(?:,[0-9]{3})+)(?:\.[0-9]{1,2})?\z", ErrorMessage = "Use a currency code and non-negative amount, for example CAD 1,800.")]
    public string DisplayPrice { get; set; } = "";
    [Required, StringLength(200)]
    public string IdealFor { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [EnumDataType(typeof(PricingTreatment))]
    public PricingTreatment Treatment { get; set; }
    public List<string> Features { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Features is null || Features.Count > 20)
            yield return new("Use at most 20 features.", [nameof(Features)]);
        else if (Features.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 200))
            yield return new("Each feature is required and must be at most 200 characters.", [nameof(Features)]);
    }
}

public sealed class ServiceFaqEditModel
{
    [Required, StringLength(200)]
    public string Question { get; set; } = "";
    [Required, StringLength(2000)]
    public string Answer { get; set; } = "";
}

public sealed class ServicesPageCtaRouteAttribute : ValidationAttribute
{
    public ServicesPageCtaRouteAttribute() => ErrorMessage = "Use a public site route, or /services#Service for the service cards.";
    public override bool IsValid(object? value) => value is string path && Regex.IsMatch(path,
        @"\A(?:/|/?(?:about|services(?:#Service)?|contact|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))\z", RegexOptions.CultureInvariant);
}
