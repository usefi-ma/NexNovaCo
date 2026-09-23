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

public sealed class GlobalSettingsService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media, ILogger<GlobalSettingsService> logger) : IGlobalSettingsService
{
    public Task<SiteIdentityEditModel> ReadSiteIdentityAsync(CancellationToken ct = default) => SafeReadAsync(() => ReadSiteIdentityAsync(false, ct), SiteIdentityEditModel.Approved, "SiteIdentity");
    public Task<SiteIdentityEditModel> GetSiteIdentityForEditAsync(CancellationToken ct = default) => ReadSiteIdentityAsync(true, ct);
    private async Task<SiteIdentityEditModel> ReadSiteIdentityAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.SiteIdentitySettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("SiteIdentity has not been initialized.");
        var model = row.ToEditModel();
        if (!admin)
        {
            Validate(model);
            model.ImagePath = media.ResolvePublicPath(model.ImagePath, MediaKind.SiteLogo);
        }
        return model;
    }
    public async Task SaveSiteIdentityAsync(SiteIdentityEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.SiteLogo);
        var row = await db.SiteIdentitySettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("SiteIdentity has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<FooterEditModel> ReadFooterAsync(CancellationToken ct = default) => SafeReadAsync(() => ReadFooterAsync(false, ct), FooterEditModel.Approved, "Footer");
    public Task<FooterEditModel> GetFooterForEditAsync(CancellationToken ct = default) => ReadFooterAsync(true, ct);
    private async Task<FooterEditModel> ReadFooterAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.FooterSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Footer has not been initialized.");
        var model = row.ToEditModel();
        if (!admin)
        {
            Validate(model);
        }
        return model;
    }
    public async Task SaveFooterAsync(FooterEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.FooterSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Footer has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogError(exception, "Global {Section} could not be read; rendering approved defaults without writes.", section);
            return fallback();
        }
    }
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);

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
