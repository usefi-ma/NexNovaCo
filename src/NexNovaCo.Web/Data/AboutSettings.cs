using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class AboutHeroSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string MobileTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutHeroEditModel ToEditModel() => new() { Title = Title, MobileTitle = MobileTitle, Description = Description, CtaLabel = CtaLabel, CtaHref = CtaHref, ImagePath = ImagePath };
    public void SetContent(AboutHeroEditModel model)
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

public sealed class AboutStorySettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string IntroductionOne { get; set; } = "";
    public string IntroductionTwo { get; set; } = "";
    public string DetailOne { get; set; } = "";
    public string DetailTwo { get; set; } = "";
    public string Closing { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutStoryEditModel ToEditModel() => new() { Title = Title, Subtitle = Subtitle, IntroductionOne = IntroductionOne, IntroductionTwo = IntroductionTwo, DetailOne = DetailOne, DetailTwo = DetailTwo, Closing = Closing };
    public void SetContent(AboutStoryEditModel model)
    {
        Title = model.Title;
        Subtitle = model.Subtitle;
        IntroductionOne = model.IntroductionOne;
        IntroductionTwo = model.IntroductionTwo;
        DetailOne = model.DetailOne;
        DetailTwo = model.DetailTwo;
        Closing = model.Closing;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutVisionSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string ParagraphOne { get; set; } = "";
    public string ParagraphTwo { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string ImageAlt { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutVisionEditModel ToEditModel() => new() { Title = Title, ParagraphOne = ParagraphOne, ParagraphTwo = ParagraphTwo, CtaLabel = CtaLabel, CtaHref = CtaHref, ImagePath = ImagePath, ImageAlt = ImageAlt };
    public void SetContent(AboutVisionEditModel model)
    {
        Title = model.Title;
        ParagraphOne = model.ParagraphOne;
        ParagraphTwo = model.ParagraphTwo;
        CtaLabel = model.CtaLabel;
        CtaHref = model.CtaHref;
        ImagePath = model.ImagePath;
        ImageAlt = model.ImageAlt;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutTimelineSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string CtaHref { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutTimelineEditModel ToEditModel() => new() { Title = Title, Description = Description, CtaLabel = CtaLabel, CtaHref = CtaHref };
    public void SetContent(AboutTimelineEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        CtaLabel = model.CtaLabel;
        CtaHref = model.CtaHref;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutMissionSettings
{
    public int Id { get; set; } = 1;
    public string BrandTitle { get; set; } = "";
    public string BrandDescription { get; set; } = "";
    public string Title { get; set; } = "";
    public string ParagraphOne { get; set; } = "";
    public string ParagraphTwo { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutMissionEditModel ToEditModel() => new() { BrandTitle = BrandTitle, BrandDescription = BrandDescription, Title = Title, ParagraphOne = ParagraphOne, ParagraphTwo = ParagraphTwo };
    public void SetContent(AboutMissionEditModel model)
    {
        BrandTitle = model.BrandTitle;
        BrandDescription = model.BrandDescription;
        Title = model.Title;
        ParagraphOne = model.ParagraphOne;
        ParagraphTwo = model.ParagraphTwo;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutPartnersSettings
{
    public int Id { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutPartnersEditModel ToEditModel() => new() { Title = Title, Description = Description };
    public void SetContent(AboutPartnersEditModel model)
    {
        Title = model.Title;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutTimelineItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public int Year { get; set; }
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutTimelineItemEditModel ToEditModel() => new() { Year = Year, Description = Description };
    public void SetContent(AboutTimelineItemEditModel model)
    {
        Year = model.Year;
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutTimelineInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AboutMissionPointItem
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public AboutMissionPointEditModel ToEditModel() => new() { Description = Description };
    public void SetContent(AboutMissionPointEditModel model)
    {
        Description = model.Description;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AboutMissionPointInitializationState
{
    public int Id { get; set; } = 1;
    public DateTime InitializedAtUtc { get; set; } = DateTime.UtcNow;
}
