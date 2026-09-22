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

public sealed class HomeServicesSectionContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<HomeServicesSectionContentService> logger) : IHomeServicesSectionContentService
{
    public async Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var settings = await database.HomeServicesSectionSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == HomeServicesSectionSettings.SingletonId, cancellationToken);
            if (settings is not null)
            {
                var content = settings.ToContent();
                var model = HomeServicesSectionEditModel.FromContent(content);
                Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
                return content;
            }
            logger.LogWarning("Home Services section record is missing; rendering approved defaults without writing to the database.");
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home Services section could not be read safely; rendering approved defaults without writing to the database.");
        }
        return HomeServicesSectionDefaults.Content;
    }

    public async Task<HomeServicesSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var settings = await database.HomeServicesSectionSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == HomeServicesSectionSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Services section has not been initialized.");
        // The editor must report read failures, never present fallback as if it were saved content.
        return HomeServicesSectionEditModel.FromContent(settings.ToContent());
    }

    public async Task UpdateAsync(HomeServicesSectionEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
        var settings = await database.HomeServicesSectionSettings.SingleOrDefaultAsync(x => x.Id == HomeServicesSectionSettings.SingletonId, cancellationToken)
            ?? throw new KeyNotFoundException("Home Services section has not been initialized.");
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
