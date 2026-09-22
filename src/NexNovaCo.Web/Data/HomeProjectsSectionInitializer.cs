using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeProjectsSectionInitializer
{
    // Controlled startup only; a public read must never create or overwrite content.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomeProjectsSectionSettings.AsNoTracking().AnyAsync(x => x.Id == HomeProjectsSectionSettings.SingletonId, cancellationToken)) return;
        var settings = new HomeProjectsSectionSettings();
        settings.SetContent(HomeProjectsSectionDefaults.Content);
        database.HomeProjectsSectionSettings.Add(settings);
        await database.SaveChangesAsync(cancellationToken);
    }
}
