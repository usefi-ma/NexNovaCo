using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeTeamSectionInitializer
{
    // Controlled startup only, matching Hero; never invoked by a public read.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomeTeamSectionSettings.AsNoTracking().AnyAsync(x => x.Id == HomeTeamSectionSettings.SingletonId, cancellationToken)) return;
        var settings = new HomeTeamSectionSettings();
        settings.SetContent(HomeTeamSectionDefaults.Content);
        database.HomeTeamSectionSettings.Add(settings);
        await database.SaveChangesAsync(cancellationToken);
    }
}
