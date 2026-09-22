using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeStatisticsInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        // A non-empty collection is never repaired or overwritten automatically.
        if (await database.HomeStatistics.AsNoTracking().AnyAsync(cancellationToken)) return;
        var now = DateTime.UtcNow;
        database.HomeStatistics.AddRange(HomeStatisticsDefaults.Content.Select((item, index) => new HomeStatistic
        {
            Id = index + 1, DisplayOrder = index + 1, Label = item.Label, Value = item.Value, UpdatedAtUtc = now
        }));
        await database.SaveChangesAsync(cancellationToken);
    }
}
