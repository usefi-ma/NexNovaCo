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

public sealed class ShopContentService(
    IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication,
    IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media,
    IProductContentService products,
    ILogger<ShopContentService> logger) : IShopContentService
{
    public async Task<ShopContent> ReadPublicAsync(CancellationToken cancellationToken = default)
    {
        var hero = await SafeReadAsync(() => ReadHeroAsync(false, cancellationToken), ShopHeroEditModel.Approved, "Hero");
        var section = await SafeReadAsync(() => ReadProductsSectionAsync(false, cancellationToken), ShopProductsSectionEditModel.Approved, "Products section");
        return new(
            new(hero.Eyebrow, hero.Title, hero.Description, hero.CtaLabel, media.ResolvePublicPath(hero.ImagePath, MediaKind.ShopHero)),
            new(section.Eyebrow, section.Title, section.Introduction),
            await products.GetAsync(cancellationToken));
    }

    public Task<ShopHeroEditModel> GetHeroForEditAsync(CancellationToken cancellationToken = default) => ReadHeroAsync(true, cancellationToken);

    public async Task SaveHeroAsync(ShopHeroEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.ShopHero);
        var row = await database.ShopHeroSettings.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
            ?? throw new KeyNotFoundException("Shop Hero has not been initialized.");
        row.SetContent(model);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task<ShopProductsSectionEditModel> GetProductsSectionForEditAsync(CancellationToken cancellationToken = default) =>
        ReadProductsSectionAsync(true, cancellationToken);

    public async Task SaveProductsSectionAsync(ShopProductsSectionEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        var row = await database.ShopProductsSectionSettings.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
            ?? throw new KeyNotFoundException("Shop Products section has not been initialized.");
        row.SetContent(model);
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<ShopHeroEditModel> ReadHeroAsync(bool admin, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        if (admin) await RequireAdminAsync(database, cancellationToken);
        var model = (await database.ShopHeroSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
            ?? throw new KeyNotFoundException("Shop Hero has not been initialized.")).ToEditModel();
        if (!admin) Validate(model);
        return model;
    }

    private async Task<ShopProductsSectionEditModel> ReadProductsSectionAsync(bool admin, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        if (admin) await RequireAdminAsync(database, cancellationToken);
        var model = (await database.ShopProductsSectionSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
            ?? throw new KeyNotFoundException("Shop Products section has not been initialized.")).ToEditModel();
        if (!admin) Validate(model);
        return model;
    }

    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "Shop {Section} could not be read; rendering approved defaults without writes.", section);
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
