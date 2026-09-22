using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Data;

public static class ServiceInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.ServiceInitializationStates.AnyAsync(cancellationToken)) return;
        if (!await database.Services.AnyAsync(cancellationToken))
        {
            var order = 0;
            foreach (var content in ServiceCatalog.All)
            {
                var entity = new ServiceEntity { ContentKey = content.Id, DisplayOrder = ++order };
                entity.SetContent(ServiceEditModel.FromContent(content));
                database.Services.Add(entity);
                var featuredOrder = ServiceCatalog.HomeFeatured.ToList().FindIndex(x => x.Id == content.Id);
                if (featuredOrder >= 0)
                    database.HomeFeaturedServices.Add(new HomeFeaturedService { Service = entity, DisplayOrder = featuredOrder + 1 });
            }
        }
        // Existing unmarked content/selection is adopted as-is, never guessed from row count.
        database.ServiceInitializationStates.Add(new ServiceInitializationState());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
