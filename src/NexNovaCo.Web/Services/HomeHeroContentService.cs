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

public sealed class HomeHeroContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<HomeHeroContentService> logger) : IHomeHeroContentService
{
    public async Task<HomeHeroContent> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var hero = await database.HomeHeroSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == HomeHeroSettings.SingletonId, cancellationToken);
            if (hero is not null)
            {
                var content = hero.ToContent();
                var model = HomeHeroEditModel.FromContent(content);
                Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
                return content;
            }
            logger.LogWarning("Home Hero record is missing; rendering approved defaults without writing to the database.");
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home Hero could not be read safely; rendering approved defaults without writing to the database.");
        }
        return HomeHeroDefaults.Content;
    }

    public async Task<HomeHeroEditModel> GetForEditAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var hero = await database.HomeHeroSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == HomeHeroSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Hero has not been initialized.");
        // The editor must report read failures, never present fallback as if it were saved content.
        return HomeHeroEditModel.FromContent(hero.ToContent());
    }

    public async Task UpdateAsync(HomeHeroEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
        var hero = await database.HomeHeroSettings.SingleOrDefaultAsync(x => x.Id == HomeHeroSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Hero has not been initialized.");
        hero.SetContent(model.ToContent());
        await database.SaveChangesAsync(cancellationToken);
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
            !await (from membership in database.UserRoles
                    join role in database.Roles on membership.RoleId equals role.Id
                    where membership.UserId == userId && role.Name == IdentityDatabaseInitializer.AdminRole
                    select membership).AnyAsync(cancellationToken))
            throw new UnauthorizedAccessException("An active Admin session is required.");
    }
}
