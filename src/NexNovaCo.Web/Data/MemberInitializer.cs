using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Data;

public static class MemberInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, MemberCatalog defaults, CancellationToken cancellationToken = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.MemberInitializationStates.AnyAsync(cancellationToken)) return;
        if (!await database.Members.AnyAsync(cancellationToken))
        {
            var order = 0;
            foreach (var content in await defaults.GetDetailsAsync(cancellationToken))
            {
                var entity = new MemberEntity { DisplayOrder = ++order };
                entity.SetContent(MemberEditModel.FromContent(content));
                database.Members.Add(entity);
                var featuredOrder = MemberCatalog.HomeFeaturedSlugs.ToList().IndexOf(content.Summary.Slug);
                if (featuredOrder >= 0)
                    database.HomeFeaturedMembers.Add(new HomeFeaturedMember { Member = entity, DisplayOrder = featuredOrder + 1 });
            }
        }
        database.MemberInitializationStates.Add(new MemberInitializationState());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
