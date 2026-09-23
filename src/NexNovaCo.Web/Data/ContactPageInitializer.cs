using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class ContactPageInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.ContactPageSettings.AnyAsync(ct))
        {
            var row = new ContactPageSettings { InfoHeading = ContactPageDefaults.Content.Info.Heading };
            row.SetHero(ContactHeroEditModel.Approved());
            row.SetMap(ContactMapEditModel.Approved());
            db.ContactPageSettings.Add(row);
        }
        if (!await db.ContactFormSettings.AnyAsync(ct))
        {
            var row = new ContactFormSettings(); row.SetForm(ContactFormEditModel.Approved());
            db.ContactFormSettings.Add(row);
        }
        if (!await db.SiteContactSettings.AnyAsync(ct))
        {
            var c = ContactPageDefaults.Content.Info;
            db.SiteContactSettings.Add(new() { Phone = c.Phone, Email = c.Email, Address = c.Address, UpdatedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
