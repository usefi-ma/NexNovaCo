using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeHeroInitializer
{
    // Called only by the existing controlled migration/startup workflow, never by public reads.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomeHeroSettings.AsNoTracking().AnyAsync(x => x.Id == HomeHeroSettings.SingletonId, cancellationToken)) return;
        var hero = new HomeHeroSettings();
        hero.SetContent(HomeHeroDefaults.Content);
        database.HomeHeroSettings.Add(hero);
        await database.SaveChangesAsync(cancellationToken);
    }
}
