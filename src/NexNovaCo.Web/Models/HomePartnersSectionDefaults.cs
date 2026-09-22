namespace NexNovaCo.Web.Models;

public static class HomePartnersSectionDefaults
{
    // Reuse the approved shared introduction for first initialization/fallback only.
    // Home edits never change About's introduction or the shared partner collection.
    public static SectionHeading Content => PartnerPresentationDefaults.Heading;
}
