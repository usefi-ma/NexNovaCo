using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class ProjectsPageInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.ProjectsHeroSettings.AnyAsync(ct))
        {
            var row = new ProjectsHeroSettings(); row.SetContent(ProjectsHeroEditModel.Approved());
            db.ProjectsHeroSettings.Add(row);
        }
        if (!await db.ProjectsTestimonialsSettings.AnyAsync(ct))
        {
            var row = new ProjectsTestimonialsSettings(); row.SetContent(ProjectsTestimonialsEditModel.Approved());
            db.ProjectsTestimonialsSettings.Add(row);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
