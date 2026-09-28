namespace NexNovaCo.Web.Models;

public sealed record ProductDetailDefault(
    string FullDescription,
    IReadOnlyList<ProductImage> Gallery,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> RelatedSlugs);

public static class ProductDetailDefaults
{
    public static IReadOnlyDictionary<string, ProductDetailDefault> BySlug { get; } =
        new Dictionary<string, ProductDetailDefault>(StringComparer.Ordinal)
        {
            ["productivity-pro"] = Detail(
                "Productivity Pro brings planning, prioritization, and reflection into one focused workspace. Use the flexible structure as-is or adapt it to the way you already work.\n\nClear sections keep daily actions connected to larger goals without adding unnecessary complexity.",
                [(0, "Productivity workspace overview"), (6, "Planning dashboard and task workflow"), (8, "Focused weekly planning view")],
                ["Structured daily and weekly planning", "Goal and priority tracking", "Reusable reflection prompts"],
                ["freelancer-business-kit", "website-ui-kit", "email-marketing-kit"]),
            ["social-media-content-kit"] = Detail(
                "The Social Media Content Kit helps turn ideas into an organized publishing rhythm. Its practical templates support planning, production, and review across the channels that matter to your team.\n\nEverything is designed to be easy to reuse, so consistent content takes less effort to manage.",
                [(1, "Social media content planning kit"), (7, "Campaign planning workspace"), (3, "Content production workflow")],
                ["Reusable campaign planning templates", "Content production checklists", "Simple publishing overview"],
                ["email-marketing-kit", "productivity-pro", "pitch-deck-template"]),
            ["pitch-deck-template"] = Detail(
                "Pitch Deck Template provides a clear narrative framework for presenting an idea, opportunity, and next step. Thoughtful layouts help the story stay readable while leaving room for your own visual identity.\n\nUse it to shape an investor presentation, internal proposal, or concise project overview.",
                [(2, "Pitch deck presentation cover"), (8, "Modern presentation layout"), (4, "Visual design system preview")],
                ["Clear presentation narrative", "Flexible modern layouts", "Easy-to-customize visual system"],
                ["website-ui-kit", "productivity-pro", "freelancer-business-kit"]),
            ["freelancer-business-kit"] = Detail(
                "Freelancer Business Kit gathers the core documents independent professionals use to run projects with clarity. Practical templates cover the client journey from first conversation through delivery.\n\nThe collection is intentionally straightforward, making it easy to adapt to different services and working styles.",
                [(3, "Freelancer business toolkit"), (0, "Client project planning workspace"), (5, "Business document dashboard")],
                ["Client-ready document templates", "Project and invoice organization", "Repeatable delivery checklists"],
                ["productivity-pro", "email-marketing-kit", "pitch-deck-template"]),
            ["website-ui-kit"] = Detail(
                "Website UI Kit is a practical starting point for building polished digital interfaces. The collection balances reusable foundations with enough flexibility to support distinct product identities.\n\nComponents are organized for quick exploration, consistent handoff, and efficient iteration.",
                [(4, "Website user interface kit"), (7, "Responsive interface concept"), (0, "Digital product workspace")],
                ["Reusable interface components", "Responsive layout foundations", "Consistent design patterns"],
                ["pitch-deck-template", "productivity-pro", "social-media-content-kit"]),
            ["email-marketing-kit"] = Detail(
                "Email Marketing Kit helps teams plan clear, purposeful campaigns without starting from a blank page. Templates support messaging, scheduling, and review while keeping audience needs at the center.\n\nUse the kit for launches, newsletters, and ongoing nurture communication.",
                [(5, "Email marketing campaign kit"), (1, "Campaign content planning tools"), (6, "Marketing workflow dashboard")],
                ["Campaign planning templates", "Message and subject-line prompts", "Reusable review workflow"],
                ["social-media-content-kit", "productivity-pro", "freelancer-business-kit"])
        };

    private static ProductDetailDefault Detail(string description, (int Image, string Alt)[] gallery,
        string[] features, string[] related) => new(description,
        gallery.Select(item => new ProductImage(ProductImageAssets.Paths[item.Image], item.Alt)).ToArray(),
        features,
        related);
}
