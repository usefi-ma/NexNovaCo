using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class HomeProjectsSectionSettings
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }

    public SectionHeading ToContent() => new(Title, Description);

    public void SetContent(SectionHeading content)
    {
        Title = content.Title;
        Description = content.Description ?? "";
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
