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

// Cohesive About-only services; no dynamic section keys, generic repository or shared-content copies.
public sealed class AboutCmsService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media, ILogger<AboutCmsService> logger) : IAboutCmsService
{
    public async Task<AboutContent> ReadPublicAsync(CancellationToken ct = default)
    {
        var h = await SafeReadAsync(() => ReadHeroAsync(false, ct), AboutHeroEditModel.Approved, "Hero");
        var s = await SafeReadAsync(() => ReadStoryAsync(false, ct), AboutStoryEditModel.Approved, "Story");
        var v = await SafeReadAsync(() => ReadVisionAsync(false, ct), AboutVisionEditModel.Approved, "Vision");
        var t = await SafeReadAsync(() => ReadTimelineAsync(false, ct), AboutTimelineEditModel.Approved, "Timeline intro");
        var m = await SafeReadAsync(() => ReadMissionAsync(false, ct), AboutMissionEditModel.Approved, "Mission");
        var p = await SafeReadAsync(() => ReadPartnersAsync(false, ct), AboutPartnersEditModel.Approved, "Partners intro");
        var timeline = await SafeReadAsync<IReadOnlyList<TimelineMilestone>>(async () =>
        {
            var rows = await ReadTimelineItemsAsync(false, ct);
            return rows.Select(x => new TimelineMilestone(x.Year, x.Description)).ToArray();
        }, () => AboutDefaults.Content.Timeline.Milestones, "Timeline");
        var points = await SafeReadAsync<IReadOnlyList<string>>(async () =>
        {
            var rows = await ReadMissionPointItemsAsync(false, ct);
            return rows.Select(x => x.Description).ToArray();
        }, () => AboutDefaults.Content.Mission.Commitments, "Mission points");
        return new(new(h.Title, h.MobileTitle, h.Description, h.CtaLabel, h.CtaHref),
            new(s.Title, s.Subtitle, [s.IntroductionOne, s.IntroductionTwo], [s.DetailOne, s.DetailTwo], s.Closing),
            new(v.Title, [v.ParagraphOne, v.ParagraphTwo], v.CtaLabel, v.CtaHref,
                media.ResolvePublicPath(v.ImagePath, MediaKind.AboutVision), v.ImageAlt),
            new(new(t.Title, t.Description), timeline, t.CtaLabel, t.CtaHref),
            new(new(m.BrandTitle, m.BrandDescription), m.Title, [m.ParagraphOne, m.ParagraphTwo], points),
            new(p.Title, p.Description), [], media.ResolvePublicPath(h.ImagePath, MediaKind.AboutHero));
    }
    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "About {Section} could not be read; rendering approved defaults without writes.", section);
            return fallback();
        }
    }
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);

    public Task<AboutHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default) => ReadHeroAsync(true, ct);
    private async Task<AboutHeroEditModel> ReadHeroAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.AboutHeroSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Hero has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveHeroAsync(AboutHeroEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.AboutHero);
        var row = await db.AboutHeroSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Hero has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<AboutStoryEditModel> GetStoryForEditAsync(CancellationToken ct = default) => ReadStoryAsync(true, ct);
    private async Task<AboutStoryEditModel> ReadStoryAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.AboutStorySettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Story has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveStoryAsync(AboutStoryEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.AboutStorySettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Story has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<AboutVisionEditModel> GetVisionForEditAsync(CancellationToken ct = default) => ReadVisionAsync(true, ct);
    private async Task<AboutVisionEditModel> ReadVisionAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.AboutVisionSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Vision has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveVisionAsync(AboutVisionEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.AboutVision);
        var row = await db.AboutVisionSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Vision has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<AboutTimelineEditModel> GetTimelineForEditAsync(CancellationToken ct = default) => ReadTimelineAsync(true, ct);
    private async Task<AboutTimelineEditModel> ReadTimelineAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.AboutTimelineSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Timeline has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveTimelineAsync(AboutTimelineEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.AboutTimelineSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Timeline has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<AboutMissionEditModel> GetMissionForEditAsync(CancellationToken ct = default) => ReadMissionAsync(true, ct);
    private async Task<AboutMissionEditModel> ReadMissionAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.AboutMissionSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Mission has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveMissionAsync(AboutMissionEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.AboutMissionSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Mission has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<AboutPartnersEditModel> GetPartnersForEditAsync(CancellationToken ct = default) => ReadPartnersAsync(true, ct);
    private async Task<AboutPartnersEditModel> ReadPartnersAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.AboutPartnersSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Partners has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SavePartnersAsync(AboutPartnersEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.AboutPartnersSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("About Partners has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<AboutTimelineItem>> ListTimelineAsync(CancellationToken ct = default) => ReadTimelineItemsAsync(true, ct);
    private async Task<IReadOnlyList<AboutTimelineItem>> ReadTimelineItemsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var rows = await db.AboutTimelineItems.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (!admin) foreach (var row in rows) Validate(row.ToEditModel());
        return rows;
    }
    public async Task<AboutTimelineItemEditModel> GetTimelineItemForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        return (await db.AboutTimelineItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.")).ToEditModel();
    }
    public async Task<int> CreateTimelineAsync(AboutTimelineItemEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var rows = await db.AboutTimelineItems.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].DisplayOrder = i + 1;
        var row = new AboutTimelineItem { DisplayOrder = rows.Count + 1 }; row.SetContent(model);
        db.AboutTimelineItems.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return row.Id;
    }
    public async Task UpdateTimelineAsync(int id, AboutTimelineItemEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var row = await db.AboutTimelineItems.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.");
        row.SetContent(model); await db.SaveChangesAsync(ct);
    }
    public async Task DeleteTimelineAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.AboutTimelineItems.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("The item no longer exists.");
        db.AboutTimelineItems.Remove(row); rows.Remove(row);
        for (var i = 0; i < rows.Count; i++) { rows[i].DisplayOrder = i + 1; rows[i].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task ReorderTimelineAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.AboutTimelineItems.ToDictionaryAsync(x => x.Id, ct);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(rows.Keys.Order()))
            throw new ValidationException("The collection changed. Refresh before reordering.");
        for (var i = 0; i < ids.Length; i++) { rows[ids[i]].DisplayOrder = i + 1; rows[ids[i]].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public Task<IReadOnlyList<AboutMissionPointItem>> ListMissionPointAsync(CancellationToken ct = default) => ReadMissionPointItemsAsync(true, ct);
    private async Task<IReadOnlyList<AboutMissionPointItem>> ReadMissionPointItemsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var rows = await db.AboutMissionPointItems.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (!admin) foreach (var row in rows) Validate(row.ToEditModel());
        return rows;
    }
    public async Task<AboutMissionPointEditModel> GetMissionPointItemForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        return (await db.AboutMissionPointItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.")).ToEditModel();
    }
    public async Task<int> CreateMissionPointAsync(AboutMissionPointEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var rows = await db.AboutMissionPointItems.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].DisplayOrder = i + 1;
        var row = new AboutMissionPointItem { DisplayOrder = rows.Count + 1 }; row.SetContent(model);
        db.AboutMissionPointItems.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return row.Id;
    }
    public async Task UpdateMissionPointAsync(int id, AboutMissionPointEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var row = await db.AboutMissionPointItems.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.");
        row.SetContent(model); await db.SaveChangesAsync(ct);
    }
    public async Task DeleteMissionPointAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.AboutMissionPointItems.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("The item no longer exists.");
        db.AboutMissionPointItems.Remove(row); rows.Remove(row);
        for (var i = 0; i < rows.Count; i++) { rows[i].DisplayOrder = i + 1; rows[i].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task ReorderMissionPointAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.AboutMissionPointItems.ToDictionaryAsync(x => x.Id, ct);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(rows.Keys.Order()))
            throw new ValidationException("The collection changed. Refresh before reordering.");
        for (var i = 0; i < ids.Length; i++) { rows[ids[i]].DisplayOrder = i + 1; rows[ids[i]].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
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
