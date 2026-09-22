using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class ServiceEditModel
{
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string Tagline { get; set; } = "";
    [Required, StringLength(200)] public string Description { get; set; } = "";
    [Required, StringLength(200), ServiceIconPath] public string IconPath { get; set; } = "";
    public static ServiceEditModel FromContent(ServiceSummary content) => new()
    {
        Name = content.Name, Tagline = content.Tagline, Description = content.Description, IconPath = content.IconPath
    };
}

public sealed record ServiceListItem(int Id, int DisplayOrder, string Name, string Tagline, string IconPath, int? HomeDisplayOrder);

public static class ServiceIconAssets
{
    public static IReadOnlyList<string> Paths { get; } = Array.AsReadOnly(new[]
    {
        "image/service/icons/online-services.png", "image/service/icons/software.png",
        "image/service/icons/analysing.png", "image/service/icons/process.png",
        "image/service/icons/custom.png", "image/service/icons/mobile-app.png"
    });
    public static bool IsAllowed(string? path) => Paths.Contains(path, StringComparer.Ordinal);
}

public sealed class ServiceIconPathAttribute : ValidationAttribute
{
    public ServiceIconPathAttribute() => ErrorMessage = "Choose an existing bundled service icon.";
    public override bool IsValid(object? value) => value is string path && ServiceIconAssets.IsAllowed(path);
}

public static class HomeFeaturedServiceRules
{
    // Preserve Home's approved two + three cards: the fixed-height desktop background
    // cannot safely accommodate extra rows at the 1025–1199px Bootstrap layout.
    public const int MaximumCount = 5;
}
