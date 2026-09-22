using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Data;

public static class PartnerInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        // Controlled startup only. Seed and marker commit together, including concurrent startup attempts.
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.PartnerInitializationStates.AnyAsync(cancellationToken)) return;

        // Adopt existing content without overwriting it when initialization has not yet been recorded.
        if (!await database.Partners.AnyAsync(cancellationToken))
        {
            var order = 0;
            foreach (var content in PartnerCatalog.All)
            {
                var entity = new PartnerEntity { DisplayOrder = ++order };
                entity.SetContent(PartnerEditModel.FromContent(content));
                database.Partners.Add(entity);
            }
        }
        database.PartnerInitializationStates.Add(new PartnerInitializationState());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
