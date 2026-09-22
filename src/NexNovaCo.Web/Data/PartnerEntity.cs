using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class PartnerEntity
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public bool HasLogoBackground { get; set; }
    public string? Href { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Partner ToContent() => new(Name, Description, ImagePath, HasLogoBackground, Href);
    public PartnerEditModel ToEditModel() => PartnerEditModel.FromContent(ToContent());
    public void SetContent(PartnerEditModel model)
    {
        var content = model.ToContent();
        Name = content.Name;
        Description = content.Description;
        ImagePath = content.ImagePath;
        HasLogoBackground = content.HasLogoBackground;
        Href = content.Href;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
