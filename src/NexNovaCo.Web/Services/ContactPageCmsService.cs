using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Contact presentation only. Demo submission remains a separate, unchanged service.
public sealed class ContactPageCmsService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media, ILogger<ContactPageCmsService> logger) : IContactPageCmsService
{
    public async Task<ContactContent> ReadPublicAsync(CancellationToken ct = default)
    {
        var hero = await SafeReadAsync(() => ReadHeroAsync(false, ct), ContactHeroEditModel.Approved, "Hero");
        var heading = await SafeReadAsync(async () => {
            await using var db = await factory.CreateDbContextAsync(ct);
            var row = await db.ContactPageSettings.AsNoTracking().SingleAsync(x => x.Id == 1, ct);
            if (string.IsNullOrWhiteSpace(row.InfoHeading) || row.InfoHeading.Length > 120) throw new ValidationException("Invalid info heading.");
            return row.InfoHeading;
        }, () => ContactPageDefaults.Content.Info.Heading, "Info heading");
        var site = await ReadSiteContactAsync(ct);
        var form = await SafeReadAsync(() => ReadFormAsync(false, ct), ContactFormEditModel.Approved, "Form");
        var map = await SafeReadAsync(() => ReadMapAsync(false, ct), ContactMapEditModel.Approved, "Map");
        return new(new(hero.Title, hero.Title, hero.Description, hero.CtaLabel, hero.CtaHref),
            new(heading, site.Phone, site.Email, site.Address),
            new(form.Heading, new(form.FirstNameLabel, form.FirstNamePlaceholder), new(form.LastNameLabel, form.LastNamePlaceholder),
                new(form.EmailLabel, form.EmailPlaceholder), new(form.SubjectLabel, form.SubjectPlaceholder),
                new(form.MessageLabel, form.MessagePlaceholder), form.SubmitLabel, form.InvalidMessage),
            new(map.Heading, map.Description, new Uri(map.EmbedUrl), map.Title),
            media.ResolvePublicPath(hero.ImagePath, MediaKind.ContactHero));
    }
    public Task<SiteContactContent> ReadSiteContactAsync(CancellationToken ct = default) =>
        SafeReadAsync(async () => {
            await using var db = await factory.CreateDbContextAsync(ct);
            var row = await db.SiteContactSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
                ?? throw new KeyNotFoundException("Site contact has not been initialized.");
            Validate(new ContactInfoEditModel { Heading = ContactPageDefaults.Content.Info.Heading, Phone = row.Phone, Email = row.Email, Address = row.Address });
            return new SiteContactContent(row.Phone, row.Email, row.Address);
        }, () => new(ContactPageDefaults.Content.Info.Phone, ContactPageDefaults.Content.Info.Email, ContactPageDefaults.Content.Info.Address), "Site contact");

    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogError(exception, "Contact {Section} could not be read; rendering approved defaults without writes.", section);
            return fallback();
        }
    }
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);

    public Task<ContactHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default) => ReadHeroAsync(true, ct);
    private async Task<ContactHeroEditModel> ReadHeroAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ContactPageSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Contact Hero has not been initialized.");
        var model = row.ToHeroModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveHeroAsync(ContactHeroEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.ContactHero);
        var row = await db.ContactPageSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Contact Hero has not been initialized.");
        row.SetHero(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ContactFormEditModel> GetFormForEditAsync(CancellationToken ct = default) => ReadFormAsync(true, ct);
    private async Task<ContactFormEditModel> ReadFormAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ContactFormSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Contact Form has not been initialized.");
        var model = row.ToFormModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveFormAsync(ContactFormEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ContactFormSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Contact Form has not been initialized.");
        row.SetForm(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ContactMapEditModel> GetMapForEditAsync(CancellationToken ct = default) => ReadMapAsync(true, ct);
    private async Task<ContactMapEditModel> ReadMapAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ContactPageSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Contact Map has not been initialized.");
        var model = row.ToMapModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveMapAsync(ContactMapEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ContactPageSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Contact Map has not been initialized.");
        row.SetMap(model);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ContactInfoEditModel> GetInfoForEditAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        var page = await db.ContactPageSettings.AsNoTracking().SingleAsync(x => x.Id == 1, ct);
        var site = await db.SiteContactSettings.AsNoTracking().SingleAsync(x => x.Id == 1, ct);
        return new() { Heading = page.InfoHeading, Phone = site.Phone, Email = site.Email, Address = site.Address };
    }
    public async Task SaveInfoAsync(ContactInfoEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var page = await db.ContactPageSettings.SingleAsync(x => x.Id == 1, ct);
        var site = await db.SiteContactSettings.SingleAsync(x => x.Id == 1, ct);
        page.InfoHeading = model.Heading;
        site.Phone = model.Phone; site.Email = model.Email; site.Address = model.Address;
        page.UpdatedAtUtc = site.UpdatedAtUtc = DateTime.UtcNow;
        // One SaveChanges transaction commits heading and canonical business data together.
        await db.SaveChangesAsync(ct);
    }

    public async Task<SiteContactEditModel> GetSiteContactForEditAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        var row = await db.SiteContactSettings.AsNoTracking().SingleAsync(x => x.Id == 1, ct);
        return new() { Phone = row.Phone, Email = row.Email, Address = row.Address };
    }
    public async Task SaveSiteContactAsync(SiteContactEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.SiteContactSettings.SingleAsync(x => x.Id == 1, ct);
        row.Phone = model.Phone; row.Email = model.Email; row.Address = model.Address;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task RequireAdminAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var claims = identityOptions.Value.ClaimsIdentity;
        var userId = principal.FindFirstValue(claims.UserIdClaimType);
        var stamp = principal.FindFirstValue(claims.SecurityStampClaimType);
        if (principal.Identity?.IsAuthenticated != true || !principal.IsInRole(IdentityDatabaseInitializer.AdminRole) ||
            userId is null || stamp is null ||
            !await database.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.SecurityStamp == stamp, cancellationToken) ||
            !await (from membership in database.UserRoles join role in database.Roles on membership.RoleId equals role.Id
                    where membership.UserId == userId && role.Name == IdentityDatabaseInitializer.AdminRole select membership).AnyAsync(cancellationToken))
            throw new UnauthorizedAccessException("An active Admin session is required.");
    }
}
