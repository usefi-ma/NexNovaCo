using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class SiteIdentitySettings
{
    public int Id { get; set; } = 1;
    public string SiteName { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public SiteIdentityEditModel ToEditModel() => new() { SiteName = SiteName, ImagePath = ImagePath };
    public void SetContent(SiteIdentityEditModel model)
    {
        SiteName = model.SiteName;
        ImagePath = model.ImagePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class FooterSettings
{
    public int Id { get; set; } = 1;
    public string Description { get; set; } = "";
    public string Copyright { get; set; } = "";
    public string NewsletterHeading { get; set; } = "";
    public string NewsletterPlaceholder { get; set; } = "";
    public string NewsletterSubmitLabel { get; set; } = "";
    public bool NewsletterVisible { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public FooterEditModel ToEditModel() => new() { Description = Description, Copyright = Copyright, NewsletterHeading = NewsletterHeading, NewsletterPlaceholder = NewsletterPlaceholder, NewsletterSubmitLabel = NewsletterSubmitLabel, NewsletterVisible = NewsletterVisible };
    public void SetContent(FooterEditModel model)
    {
        Description = model.Description;
        Copyright = model.Copyright;
        NewsletterHeading = model.NewsletterHeading;
        NewsletterPlaceholder = model.NewsletterPlaceholder;
        NewsletterSubmitLabel = model.NewsletterSubmitLabel;
        NewsletterVisible = model.NewsletterVisible;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class NavigationItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public NavigationEditModel ToEditModel() => new() { Label = Label, Url = Url };
    public void SetContent(NavigationEditModel model)
    {
        Label = model.Label;
        Url = model.Url;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class NavigationInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; }
}

public sealed class SocialLinkItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public SocialPlatform Platform { get; set; }
    public string Url { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public SocialLinkEditModel ToEditModel() => new() { Platform = Platform, Url = Url };
    public void SetContent(SocialLinkEditModel model)
    {
        Platform = model.Platform;
        Url = model.Url ?? "";
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class SocialLinkInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; }
}
