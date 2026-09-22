using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Data;

public static class TestimonialInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, CancellationToken cancellationToken = default)
    {
        // Controlled startup only. The transaction serializes simultaneous empty-collection initialization.
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (!await database.Testimonials.AnyAsync(cancellationToken))
        {
            var order = 0;
            foreach (var content in TestimonialCatalog.All)
            {
                var entity = new TestimonialEntity { DisplayOrder = ++order };
                entity.SetContent(TestimonialEditModel.FromContent(content));
                database.Testimonials.Add(entity);
            }
            await database.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
