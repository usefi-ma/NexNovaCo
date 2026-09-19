using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>
/// Read-only editorial snapshot of service.html. Replace this DI implementation when a real
/// content source exists; rendering components do not depend on storage or legacy HTML.
/// Prices and claims are preserved demo content, not a live commercial offering.
/// </summary>
public sealed class ServicesContentService : IServicesContentService
{
    private static readonly ServicesContent Content = new(
        new("What We Offer", "Our Services",
            "We specialize in delivering custom software solutions, web and mobile app development, and AI-powered innovations that are tailored to help businesses achieve their goals.",
            "Explore Our Services", "services#Service"),
        ServiceCatalog.All,
        new("Benefits and Features", "We combine cutting-edge technology with innovative strategies to deliver solutions that drive success. Our benefits and features are designed to enhance efficiency, improve decision-making, and maximize business growth. From seamless integrations to data-driven insights, we empower businesses with the tools they need to stay ahead in a competitive market."),
        [new("Innovative Solutions", "We harness the latest technology and industry insights to develop cutting-edge solutions that keep your business ahead of the competition."),
         new("Seamless Integration", "Our designs focus on user-friendly interfaces and seamless navigation, ensuring a hassle-free experience for your customers and boosting engagement."),
         new("Data-Driven Growth", "We leverage analytics and strategic insights to help you make informed decisions, optimize performance, and drive long-term success.")],
        new("HOW IT WORKS", "A step-by-step process to bring your ideas to life."),
        [new(1, "Discover & Analyze", ProcessIcon.Search),
         new(2, "Plan & Prototype", ProcessIcon.Flask),
         new(3, "Develop & Optimize", ProcessIcon.Wrench),
         new(4, "Refine & Improve", ProcessIcon.Tools),
         new(5, "Launch & Scale", ProcessIcon.Rocket)],
        new("Pricing", "Choose the perfect package to meet your needs and budget with our tailored pricing plans."),
        [new("Basic", "Essential Kickstart", "CAD 500",
            ["One-page landing website", "Mobile & desktop responsive design", "Basic contact form integration"],
            "Startups & freelancers", "Get started", PricingTreatment.Basic),
         new("Standard", "Growth-Focused Package", "CAD 900",
            ["Multi-page website (up to 5 pages)", "SEO-friendly structure", "Analytics & tracking setup"],
            "Small businesses & entrepreneurs", "Get started", PricingTreatment.Standard),
         new("Premium", "Tailored Digital Experience", "CAD 1,800",
            ["Custom website design", "Advanced CMS integration", "Enhanced SEO and performance"],
            "Growing brands & enterprises", "Get started", PricingTreatment.Premium)],
        new("FAQ", "Have questions? Our FAQ section covers everything you need to know about our services, timelines, and processes—so you can make informed decisions with confidence."),
        [new("What services does NexNovaCo offer?", "We specialize in web design, development, and digital solutions tailored to your business needs. Our services include custom website design, UX/UI development, SEO optimization, and performance enhancements."),
         new("How long does it take to complete a project?", "Project timelines vary based on complexity and requirements. A basic website can take around 2-4 weeks, while more advanced solutions may take 6-8 weeks. We provide a detailed timeline during the initial consultation."),
         new("Do you offer custom solutions, or do you use templates?", "At NexNovaCo, we believe in fully custom solutions. We do not use pre-made templates—every design and development project is built from scratch to match your brand's identity and goals."),
         new("Can you redesign an existing website?", "Yes! If you already have a website but need a fresh look or improved functionality, we can revamp it to enhance user experience, performance, and branding."),
         new("Will my website be mobile-friendly?", "Absolutely! Every website we create is responsive, ensuring seamless performance across all devices, including desktops, tablets, and smartphones."),
         new("Do you provide SEO services?", "Yes! Our Standard and Premium packages include basic SEO optimization to help your website rank better on search engines. We also offer advanced SEO services upon request.")]);

    public Task<ServicesContent> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Content);
    }
}
