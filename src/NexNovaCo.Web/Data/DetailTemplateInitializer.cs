using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class DetailTemplateInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.ProjectDetailTemplateSettings.AnyAsync(x => x.Id == 1, ct))
        {
            var row = new ProjectDetailTemplateSettings();
            row.SetContent(ProjectDetailTemplateEditModel.Approved());
            db.ProjectDetailTemplateSettings.Add(row);
        }
        if (!await db.MemberDetailTemplateSettings.AnyAsync(x => x.Id == 1, ct))
        {
            var row = new MemberDetailTemplateSettings();
            row.SetContent(MemberDetailTemplateEditModel.Approved());
            db.MemberDetailTemplateSettings.Add(row);
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
