using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Models;

public static class HomePartnersSectionDefaults
{
    // Reuse the approved shared introduction for first initialization/fallback only.
    // Home edits never change PartnerCatalog.Heading (still used by About) or its partner collection.
    public static SectionHeading Content => PartnerCatalog.Heading;
}
