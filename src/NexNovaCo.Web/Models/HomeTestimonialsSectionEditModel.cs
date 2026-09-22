using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class HomeTestimonialsSectionEditModel
{
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";

    public static HomeTestimonialsSectionEditModel FromContent(SectionHeading content) => new()
    {
        Title = content.Title, Description = content.Description ?? ""
    };

    public SectionHeading ToContent() => new(Title, Description);
}
