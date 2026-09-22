using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class HomeTestimonialsSectionInitializer
{
    // Controlled startup only; a public read must never create or overwrite content.
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.HomeTestimonialsSectionSettings.AsNoTracking().AnyAsync(x => x.Id == HomeTestimonialsSectionSettings.SingletonId, cancellationToken)) return;
        var settings = new HomeTestimonialsSectionSettings();
        settings.SetContent(HomeTestimonialsSectionDefaults.Content);
        database.HomeTestimonialsSectionSettings.Add(settings);
        await database.SaveChangesAsync(cancellationToken);
    }
}
