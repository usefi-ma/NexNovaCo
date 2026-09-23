using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class TeamPageInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.TeamHeroSettings.AnyAsync(ct))
        {
            var row = new TeamHeroSettings(); row.SetContent(TeamHeroEditModel.Approved());
            db.TeamHeroSettings.Add(row);
        }
        if (!await db.TeamSectionSettings.AnyAsync(ct))
        {
            var row = new TeamSectionSettings(); row.SetContent(TeamSectionEditModel.Approved());
            db.TeamSectionSettings.Add(row);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
