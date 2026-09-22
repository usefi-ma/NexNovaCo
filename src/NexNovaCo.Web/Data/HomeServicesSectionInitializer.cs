using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeServicesSectionInitializer
{
    // Controlled startup only; a public read must never create or overwrite content.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomeServicesSectionSettings.AsNoTracking().AnyAsync(x => x.Id == HomeServicesSectionSettings.SingletonId, cancellationToken)) return;
        var settings = new HomeServicesSectionSettings();
        settings.SetContent(HomeServicesSectionDefaults.Content);
        database.HomeServicesSectionSettings.Add(settings);
        await database.SaveChangesAsync(cancellationToken);
    }
}
