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

public sealed class HomeTeamSectionContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<HomeTeamSectionContentService> logger) : IHomeTeamSectionContentService
{
    public async Task<TeamSectionContent> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var settings = await database.HomeTeamSectionSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == HomeTeamSectionSettings.SingletonId, cancellationToken);
            if (settings is not null)
            {
                var content = settings.ToContent();
                var model = HomeTeamSectionEditModel.FromContent(content);
                Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
                return content;
            }
            logger.LogWarning("Home Team record is missing; rendering approved defaults without writing to the database.");
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home Team could not be read safely; rendering approved defaults without writing to the database.");
        }
        return HomeTeamSectionDefaults.Content;
    }

    public async Task<HomeTeamSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var settings = await database.HomeTeamSectionSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == HomeTeamSectionSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Team has not been initialized.");
        // The editor must report read failures, never present fallback as if it were saved content.
        return HomeTeamSectionEditModel.FromContent(settings.ToContent());
    }

    public async Task UpdateAsync(HomeTeamSectionEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
        var settings = await database.HomeTeamSectionSettings.SingleOrDefaultAsync(x => x.Id == HomeTeamSectionSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Team has not been initialized.");
        settings.SetContent(model.ToContent());
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
