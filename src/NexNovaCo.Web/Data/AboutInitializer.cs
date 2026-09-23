using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class AboutInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.AboutHeroSettings.AnyAsync(ct))
        {
            var row = new AboutHeroSettings(); row.SetContent(AboutHeroEditModel.Approved());
            db.AboutHeroSettings.Add(row);
        }
        if (!await db.AboutStorySettings.AnyAsync(ct))
        {
            var row = new AboutStorySettings(); row.SetContent(AboutStoryEditModel.Approved());
            db.AboutStorySettings.Add(row);
        }
        if (!await db.AboutVisionSettings.AnyAsync(ct))
        {
            var row = new AboutVisionSettings(); row.SetContent(AboutVisionEditModel.Approved());
            db.AboutVisionSettings.Add(row);
        }
        if (!await db.AboutTimelineSettings.AnyAsync(ct))
        {
            var row = new AboutTimelineSettings(); row.SetContent(AboutTimelineEditModel.Approved());
            db.AboutTimelineSettings.Add(row);
        }
        if (!await db.AboutMissionSettings.AnyAsync(ct))
        {
            var row = new AboutMissionSettings(); row.SetContent(AboutMissionEditModel.Approved());
            db.AboutMissionSettings.Add(row);
        }
        if (!await db.AboutPartnersSettings.AnyAsync(ct))
        {
            var row = new AboutPartnersSettings(); row.SetContent(AboutPartnersEditModel.Approved());
            db.AboutPartnersSettings.Add(row);
        }
        if (!await db.AboutTimelineInitializationStates.AnyAsync(ct))
        {
            // An unmarked existing collection is adopted; marked intentional emptiness is permanent.
            if (!await db.AboutTimelineItems.AnyAsync(ct))
            {
                var order = 0;
                foreach (var item in AboutDefaults.Content.Timeline.Milestones)
                {
                    var row = new AboutTimelineItem { DisplayOrder = ++order };
                    row.SetContent(new() { Year = item.Year, Description = item.Description });
                    db.AboutTimelineItems.Add(row);
                }
            }
            db.AboutTimelineInitializationStates.Add(new());
        }
        if (!await db.AboutMissionPointInitializationStates.AnyAsync(ct))
        {
            // An unmarked existing collection is adopted; marked intentional emptiness is permanent.
            if (!await db.AboutMissionPointItems.AnyAsync(ct))
            {
                var order = 0;
                foreach (var item in AboutDefaults.Content.Mission.Commitments)
                {
                    var row = new AboutMissionPointItem { DisplayOrder = ++order };
                    row.SetContent(new() { Description = item });
                    db.AboutMissionPointItems.Add(row);
                }
            }
            db.AboutMissionPointInitializationStates.Add(new());
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
