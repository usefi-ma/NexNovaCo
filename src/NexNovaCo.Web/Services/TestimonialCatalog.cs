using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Identical approved Home/Projects editorial content; no invented client records or destinations.
internal static class TestimonialCatalog
{
    public static IReadOnlyList<Testimonial> All { get; } = Array.AsReadOnly<Testimonial>([
        new("Olivia Carter, COO at Alpha Co",
            ["Partnering with NexNovaCo was a game-changer for our business. Their AI-driven web and mobile solutions streamlined our operations and gave us a competitive edge. The team is professional, responsive, and truly innovative — we saw measurable growth within just months of implementation.",
             "We were particularly impressed by their attention to detail and ability to translate our complex requirements into user-friendly, scalable software. NexNovaCo didn't just deliver a solution — they delivered real value."]),
        new("Daniel Kim, Marketing Director at Tech Co",
            ["Working with NexNovaCo transformed our digital presence. Their AI-based analytics tools gave us deep insights into customer behavior, helping us improve engagement and retention dramatically. The process was smooth and collaborative from start to finish.",
             "What stood out most was their commitment to quality and their genuine passion for innovation. NexNovaCo became more than a vendor — they became a strategic partner in our growth."])
    ]);
    public static SectionHeading Brand { get; } = new("NexNovaCo",
        "NexNovaCo delivers innovative AI-driven, web, and mobile solutions, empowering businesses with cutting-edge technology for growth and success.");
}
