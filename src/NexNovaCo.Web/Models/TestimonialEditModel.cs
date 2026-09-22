using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class TestimonialEditModel
{
    [Required, StringLength(180)] public string Attribution { get; set; } = "";
    [Required, StringLength(1600)] public string Quote { get; set; } = "";

    // Preserve the existing public contract: one attribution and an ordered paragraph collection.
    public Testimonial ToContent() => new(Attribution.Trim(),
        Regex.Split(Quote.Replace("\r\n", "\n").Replace('\r', '\n').Trim(), @"\n\s*\n")
            .Select(paragraph => paragraph.Trim()).Where(paragraph => paragraph.Length > 0).ToArray());

    public static TestimonialEditModel FromContent(Testimonial content) => new()
    {
        Attribution = content.Attribution, Quote = string.Join("\n\n", content.Paragraphs)
    };
}

public sealed record TestimonialListItem(int Id, int DisplayOrder, string Attribution, string Quote);
