namespace NexNovaCo.Web.Models;

public sealed record InnerPageHeroContent(string Title, string MobileTitle, string Description,
    string CtaLabel, string CtaHref);

public sealed record BenefitContent(string Title, string Description);

public enum ProcessIcon { Search, Flask, Wrench, Tools, Rocket }
public sealed record ProcessStepContent(int Number, string Title, ProcessIcon Icon);

// The three named treatments map to the approved theme, not arbitrary style controls.
public enum PricingTreatment { Basic, Standard, Premium }
public sealed record PricingPlan(string Name, string Subtitle, string DisplayPrice,
    IReadOnlyList<string> Features, string IdealFor, string CtaLabel, PricingTreatment Treatment);

public sealed record FaqContent(string Question, string Answer);

public sealed record ServicesContent(
    InnerPageHeroContent Hero,
    IReadOnlyList<ServiceSummary> Services,
    SectionHeading BenefitsHeading,
    IReadOnlyList<BenefitContent> Benefits,
    SectionHeading ProcessHeading,
    IReadOnlyList<ProcessStepContent> Steps,
    SectionHeading PricingHeading,
    IReadOnlyList<PricingPlan> Plans,
    SectionHeading FaqHeading,
    IReadOnlyList<FaqContent> Questions);
