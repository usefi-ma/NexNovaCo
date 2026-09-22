using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomePartnersSectionInitializer
{
    // Controlled startup only; a public read must never create or overwrite content.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomePartnersSectionSettings.AsNoTracking().AnyAsync(x => x.Id == HomePartnersSectionSettings.SingletonId, cancellationToken)) return;
        var settings = new HomePartnersSectionSettings();
        settings.SetContent(HomePartnersSectionDefaults.Content);
        database.HomePartnersSectionSettings.Add(settings);
        await database.SaveChangesAsync(cancellationToken);
    }
}
