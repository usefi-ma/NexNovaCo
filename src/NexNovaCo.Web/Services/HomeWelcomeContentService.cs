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

public sealed class HomeWelcomeContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<HomeWelcomeContentService> logger) : IHomeWelcomeContentService
{
    public async Task<WelcomeContent> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var welcome = await database.HomeWelcomeSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == HomeWelcomeSettings.SingletonId, cancellationToken);
            if (welcome is not null)
            {
                var content = welcome.ToContent();
                var model = HomeWelcomeEditModel.FromContent(content);
                Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
                return content;
            }
            logger.LogWarning("Home Welcome record is missing; rendering approved defaults without writing to the database.");
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home Welcome could not be read safely; rendering approved defaults without writing to the database.");
        }
        return HomeWelcomeDefaults.Content;
    }

    public async Task<HomeWelcomeEditModel> GetForEditAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var welcome = await database.HomeWelcomeSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == HomeWelcomeSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Welcome has not been initialized.");
        // The editor must report read failures, never present fallback as if it were saved content.
        return HomeWelcomeEditModel.FromContent(welcome.ToContent());
    }

    public async Task UpdateAsync(HomeWelcomeEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
        var welcome = await database.HomeWelcomeSettings.SingleOrDefaultAsync(x => x.Id == HomeWelcomeSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Welcome has not been initialized.");
        welcome.SetContent(model.ToContent());
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
