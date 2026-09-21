using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeWelcomeInitializer
{
    // Controlled startup only, matching Hero; never invoked by a public read.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomeWelcomeSettings.AsNoTracking().AnyAsync(x => x.Id == HomeWelcomeSettings.SingletonId, cancellationToken)) return;
        var welcome = new HomeWelcomeSettings();
        welcome.SetContent(HomeWelcomeDefaults.Content);
        database.HomeWelcomeSettings.Add(welcome);
        await database.SaveChangesAsync(cancellationToken);
    }
}
