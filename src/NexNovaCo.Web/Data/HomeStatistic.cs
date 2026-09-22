using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class HomeStatistic
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Label { get; set; } = "";
    public int Value { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Statistic ToContent() => new(Label, Value);
}
