using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class ServiceEntity
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    // Existing public identity (including the Strategy icon treatment), not a route/slug.
    // New records receive an immutable generated key; editors cannot change it.
    public string ContentKey { get; set; } = "service-" + Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconPath { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public HomeFeaturedService? HomeFeatured { get; set; }

    public ServiceSummary ToContent() => new(ContentKey, Name, Tagline, Description, IconPath);
    public ServiceEditModel ToEditModel() => ServiceEditModel.FromContent(ToContent());
    public void SetContent(ServiceEditModel model)
    {
        Name = model.Name.Trim(); Tagline = model.Tagline.Trim();
        Description = model.Description.Trim(); IconPath = model.IconPath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class HomeFeaturedService
{
    public int ServiceId { get; set; }
    public int DisplayOrder { get; set; }
    public ServiceEntity Service { get; set; } = null!;
}

// Catalog and initial selection are committed together; neither is reseeded after this marker.
public sealed class ServiceInitializationState
{
    public int Id { get; set; } = 1;
}
