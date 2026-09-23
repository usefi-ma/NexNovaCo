using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class GlobalSiteInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.SiteIdentitySettings.AnyAsync(ct))
        {
            var row = new SiteIdentitySettings(); row.SetContent(SiteIdentityEditModel.Approved());
            db.SiteIdentitySettings.Add(row);
        }
        if (!await db.FooterSettings.AnyAsync(ct))
        {
            var row = new FooterSettings(); row.SetContent(FooterEditModel.Approved());
            db.FooterSettings.Add(row);
        }
        if (!await db.NavigationInitializationStates.AnyAsync(ct))
        {
            // Adopt pre-existing rows; a marker, never row count alone, owns initialization.
            if (!await db.NavigationItems.AnyAsync(ct))
                foreach (var item in GlobalSiteDefaults.Navigation)
                    db.NavigationItems.Add(new() { DisplayOrder = item.DisplayOrder, Label = item.Label, Url = item.Url, UpdatedAtUtc = DateTime.UtcNow });
            db.NavigationInitializationStates.Add(new() { InitializedAtUtc = DateTime.UtcNow });
        }
        if (!await db.SocialLinkInitializationStates.AnyAsync(ct))
        {
            // Adopt pre-existing rows; a marker, never row count alone, owns initialization.
            if (!await db.SocialLinkItems.AnyAsync(ct))
                foreach (var item in GlobalSiteDefaults.SocialLinks)
                    db.SocialLinkItems.Add(new() { DisplayOrder = item.DisplayOrder, Platform = item.Platform, Url = item.Url ?? "", UpdatedAtUtc = DateTime.UtcNow });
            db.SocialLinkInitializationStates.Add(new() { InitializedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
