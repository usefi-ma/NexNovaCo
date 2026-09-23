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

// Page settings only; member cards, details and Home selections remain in the shared member catalog.
public sealed class TeamPageCmsService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media, ILogger<TeamPageCmsService> logger) : ITeamPageCmsService
{
    public async Task<TeamContent> ReadPublicAsync(CancellationToken ct = default)
    {
        var hero = await SafeReadAsync(() => ReadHeroAsync(false, ct), TeamHeroEditModel.Approved, "Hero");
        var section = await SafeReadAsync(() => ReadSectionAsync(false, ct), TeamSectionEditModel.Approved, "Team Section");
        return new(new(hero.Title, hero.Title, hero.Description, hero.CtaLabel, hero.CtaHref),
            section.Eyebrow, new(section.Title, section.Introduction, section.Highlight, section.Description, section.CtaLabel, section.CtaHref), [],
            media.ResolvePublicPath(hero.ImagePath, MediaKind.TeamHero),
            media.ResolvePublicPath(section.ImagePath, MediaKind.TeamSection));
    }
    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "Team {Section} could not be read; rendering approved defaults without writes.", section);
            return fallback();
        }
    }
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);

    public Task<TeamHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default) => ReadHeroAsync(true, ct);
    private async Task<TeamHeroEditModel> ReadHeroAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.TeamHeroSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Team Hero has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveHeroAsync(TeamHeroEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.TeamHero);
        var row = await db.TeamHeroSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Team Hero has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<TeamSectionEditModel> GetSectionForEditAsync(CancellationToken ct = default) => ReadSectionAsync(true, ct);
    private async Task<TeamSectionEditModel> ReadSectionAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.TeamSectionSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Team Section has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveSectionAsync(TeamSectionEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.TeamSection);
        var row = await db.TeamSectionSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Team Section has not been initialized.");
        row.SetContent(model);
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
