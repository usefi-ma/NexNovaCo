using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Models;

public sealed class HomeStatisticEditModel
{
    public int Id { get; init; }
    [Required, StringLength(32)] public string Label { get; set; } = "";
    // Preserve the current four-digit counter footprint; JS represents this range exactly.
    [Required, Range(0, 9999)] public int? Value { get; set; }
}

public sealed class HomeStatisticsEditModel : IValidatableObject
{
    public List<HomeStatisticEditModel> Items { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items is null || Items.Count != HomeStatisticsDefaults.Content.Count ||
            Items.Where((item, index) => item is null || item.Id != index + 1).Any())
        {
            yield return new ValidationResult("The four existing statistics must retain their original identities and order.");
            yield break;
        }
        for (var index = 0; index < Items.Count; index++)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(Items[index], new ValidationContext(Items[index]), results, true);
            foreach (var result in results)
                yield return new ValidationResult($"Statistic {index + 1}: {result.ErrorMessage}",
                    result.MemberNames.Select(name => $"Items[{index}].{name}"));
        }
    }

    public static HomeStatisticsEditModel FromContent(IReadOnlyList<Statistic> content) => new()
    {
        Items = content.Select((item, index) => new HomeStatisticEditModel
            { Id = index + 1, Label = item.Label, Value = item.Value }).ToList()
    };
}
