using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ServicesHeroSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string MobileTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ServicesHeroEditModel ToEditModel() => new() { Title = Title, MobileTitle = MobileTitle, Description = Description, CtaLabel = CtaLabel, CtaHref = CtaHref, ImagePath = ImagePath };
    public void SetContent(ServicesHeroEditModel model)
    {
        Title = model.Title;
        MobileTitle = model.MobileTitle;
        Description = model.Description;
        CtaLabel = model.CtaLabel;
        CtaHref = model.CtaHref;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServicesBenefitsSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ServicesBenefitsEditModel ToEditModel() => new() { Title = Title, Description = Description, ImagePath = ImagePath };
    public void SetContent(ServicesBenefitsEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServicesProcessSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ServicesProcessEditModel ToEditModel() => new() { Title = Title, Description = Description };
    public void SetContent(ServicesProcessEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServicesPricingSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ServicesPricingEditModel ToEditModel() => new() { Title = Title, Description = Description };
    public void SetContent(ServicesPricingEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServicesFaqSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ServicesFaqEditModel ToEditModel() => new() { Title = Title, Description = Description };
    public void SetContent(ServicesFaqEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServiceBenefitItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public ServiceBenefitEditModel ToEditModel() => new() { Title = Title, Description = Description };
    public BenefitContent ToContent() => new(Title, Description);
    public void SetContent(ServiceBenefitEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServiceBenefitInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ServiceProcessStepItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string Title { get; set; } = "";
    public ProcessIcon Icon { get; set; }
    public ServiceProcessEditModel ToEditModel() => new() { Title = Title, Icon = Icon };
    public ProcessStepContent ToContent() => new(DisplayOrder, Title, Icon);
    public void SetContent(ServiceProcessEditModel model)
    {
        Title = model.Title;
        Icon = model.Icon;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServiceProcessInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ServicePricingPlanItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string Name { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string DisplayPrice { get; set; } = "";
    public string IdealFor { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public PricingTreatment Treatment { get; set; }
    public List<ServicePricingPlanFeature> Features { get; set; } = [];
    public ServicePricingEditModel ToEditModel() => new() { Name = Name, Subtitle = Subtitle, DisplayPrice = DisplayPrice, IdealFor = IdealFor, CtaLabel = CtaLabel, Treatment = Treatment, Features = Features.OrderBy(x => x.DisplayOrder).Select(x => x.Text).ToList() };
    public PricingPlan ToContent() => new(Name, Subtitle, DisplayPrice, Features.OrderBy(x => x.DisplayOrder).Select(x => x.Text).ToArray(), IdealFor, CtaLabel, Treatment);
    public void SetContent(ServicePricingEditModel model)
    {
        Name = model.Name;
        Subtitle = model.Subtitle;
        DisplayPrice = model.DisplayPrice;
        IdealFor = model.IdealFor;
        CtaLabel = model.CtaLabel;
        Treatment = model.Treatment;
        Features.Clear();
        Features.AddRange(model.Features.Select((text, index) => new ServicePricingPlanFeature { Text = text, DisplayOrder = index + 1 }));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServicePricingInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ServiceFaqItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public ServiceFaqEditModel ToEditModel() => new() { Question = Question, Answer = Answer };
    public FaqContent ToContent() => new(Question, Answer);
    public void SetContent(ServiceFaqEditModel model)
    {
        Question = model.Question;
        Answer = model.Answer;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ServiceFaqInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ServicePricingPlanFeature
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public int DisplayOrder { get; set; }
    public string Text { get; set; } = "";
}
