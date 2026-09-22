namespace NexNovaCo.Web.Models;

public static class HomeStatisticsDefaults
{
    public static IReadOnlyList<Statistic> Content { get; } = Array.AsReadOnly<Statistic>(
        [new("PROJECTS", 450), new("CLIENTS", 3000), new("EMPLOYEES", 1000), new("AWARDS", 26)]);
}
