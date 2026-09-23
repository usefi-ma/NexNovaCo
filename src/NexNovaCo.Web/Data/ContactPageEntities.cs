using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ContactPageSettings
{
    public int Id { get; set; } = 1;
    public string HeroTitle { get; set; } = "";
    public string HeroDescription { get; set; } = "";
    public string HeroCtaLabel { get; set; } = "";
    public string HeroCtaHref { get; set; } = "";
    public string HeroImagePath { get; set; } = "";
    public string InfoHeading { get; set; } = "";
    public string MapHeading { get; set; } = "";
    public string MapDescription { get; set; } = "";
    public string MapEmbedUrl { get; set; } = "";
    public string MapTitle { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ContactHeroEditModel ToHeroModel() => new() { Title = HeroTitle, Description = HeroDescription, CtaLabel = HeroCtaLabel, CtaHref = HeroCtaHref, ImagePath = HeroImagePath };
    public void SetHero(ContactHeroEditModel model)
    {
        HeroTitle = model.Title;
        HeroDescription = model.Description;
        HeroCtaLabel = model.CtaLabel;
        HeroCtaHref = model.CtaHref;
        HeroImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    public ContactMapEditModel ToMapModel() => new() { Heading = MapHeading, Description = MapDescription, EmbedUrl = MapEmbedUrl, Title = MapTitle };
    public void SetMap(ContactMapEditModel model)
    {
        MapHeading = model.Heading;
        MapDescription = model.Description;
        MapEmbedUrl = model.EmbedUrl;
        MapTitle = model.Title;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ContactFormSettings
{
    public int Id { get; set; } = 1;
    public string Heading { get; set; } = "";
    public string FirstNameLabel { get; set; } = "";
    public string FirstNamePlaceholder { get; set; } = "";
    public string LastNameLabel { get; set; } = "";
    public string LastNamePlaceholder { get; set; } = "";
    public string EmailLabel { get; set; } = "";
    public string EmailPlaceholder { get; set; } = "";
    public string SubjectLabel { get; set; } = "";
    public string SubjectPlaceholder { get; set; } = "";
    public string MessageLabel { get; set; } = "";
    public string MessagePlaceholder { get; set; } = "";
    public string SubmitLabel { get; set; } = "";
    public string InvalidMessage { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public ContactFormEditModel ToFormModel() => new() { Heading = Heading, FirstNameLabel = FirstNameLabel, FirstNamePlaceholder = FirstNamePlaceholder, LastNameLabel = LastNameLabel, LastNamePlaceholder = LastNamePlaceholder, EmailLabel = EmailLabel, EmailPlaceholder = EmailPlaceholder, SubjectLabel = SubjectLabel, SubjectPlaceholder = SubjectPlaceholder, MessageLabel = MessageLabel, MessagePlaceholder = MessagePlaceholder, SubmitLabel = SubmitLabel, InvalidMessage = InvalidMessage };
    public void SetForm(ContactFormEditModel model)
    {
        Heading = model.Heading;
        FirstNameLabel = model.FirstNameLabel;
        FirstNamePlaceholder = model.FirstNamePlaceholder;
        LastNameLabel = model.LastNameLabel;
        LastNamePlaceholder = model.LastNamePlaceholder;
        EmailLabel = model.EmailLabel;
        EmailPlaceholder = model.EmailPlaceholder;
        SubjectLabel = model.SubjectLabel;
        SubjectPlaceholder = model.SubjectPlaceholder;
        MessageLabel = model.MessageLabel;
        MessagePlaceholder = model.MessagePlaceholder;
        SubmitLabel = model.SubmitLabel;
        InvalidMessage = model.InvalidMessage;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class SiteContactSettings
{
    public int Id { get; set; } = 1;
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}
