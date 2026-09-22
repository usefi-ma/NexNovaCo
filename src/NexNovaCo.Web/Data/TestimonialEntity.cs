using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class TestimonialEntity
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Attribution { get; set; } = "";
    public string Quote { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }

    public TestimonialEditModel ToEditModel() => new() { Attribution = Attribution, Quote = Quote };
    public Testimonial ToContent() => ToEditModel().ToContent();
    public void SetContent(TestimonialEditModel model)
    {
        var normalized = TestimonialEditModel.FromContent(model.ToContent());
        Attribution = normalized.Attribution;
        Quote = normalized.Quote;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
