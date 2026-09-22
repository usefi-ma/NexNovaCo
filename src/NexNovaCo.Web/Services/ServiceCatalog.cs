using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

/// <summary>Approved one-time initialization and read-failure fallback only. SQLite is the live catalog.</summary>
internal static class ServiceCatalog
{
    public static IReadOnlyList<ServiceSummary> All { get; } = Array.AsReadOnly<ServiceSummary>([
        new("software", "Software Dev", "Tailored Solutions", "We design software solutions that are perfectly aligned with your business needs.", "image/service/icons/online-services.png"),
        new("web-mobile", "Web & Mobile App", "High-Performance Applications", "We develop responsive, high-performance web and mobile apps for seamless user experiences.", "image/service/icons/software.png"),
        new("ai", "AI Solutions", "Intelligent Automation", "We integrate AI to automate processes and improve business efficiency.", "image/service/icons/analysing.png"),
        new("consulting", "Tech Consulting", "Future-Ready Solutions", "We provide expert tech consulting to help businesses adopt innovative technologies.", "image/service/icons/process.png"),
        new("design", "UX/UI Design", "User-Centric Designs", "We design intuitive, user-friendly interfaces for seamless and enjoyable experiences.", "image/service/icons/custom.png"),
        new("strategy", "Strategy", "Smart Planning", "Crafting innovative, data-driven strategies that are tailored to your unique business goals.", "image/service/icons/mobile-app.png")
    ]);

    public static IReadOnlyList<ServiceSummary> HomeFeatured { get; } = Array.AsReadOnly(
        new[] { "software", "web-mobile", "ai", "consulting", "design" }
            .Select(id => All.Single(service => service.Id == id)).ToArray());
}
