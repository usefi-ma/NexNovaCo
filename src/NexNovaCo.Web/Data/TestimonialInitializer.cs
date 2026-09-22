using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Data;

public static class TestimonialInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        // Controlled startup only. Seed and marker commit together, including concurrent startup attempts.
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.TestimonialInitializationStates.AnyAsync(cancellationToken)) return;

        // Adopt existing content without overwriting it when initialization has not yet been recorded.
        if (!await database.Testimonials.AnyAsync(cancellationToken))
        {
            var order = 0;
            foreach (var content in TestimonialCatalog.All)
            {
                var entity = new TestimonialEntity { DisplayOrder = ++order };
                entity.SetContent(TestimonialEditModel.FromContent(content));
                database.Testimonials.Add(entity);
            }
        }
        database.TestimonialInitializationStates.Add(new TestimonialInitializationState());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
