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

public sealed class HomeStatisticsContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<HomeStatisticsContentService> logger) : IHomeStatisticsContentService
{
    public async Task<IReadOnlyList<Statistic>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await ReadAsync(database, cancellationToken);
            ValidateRows(rows);
            return rows.Select(row => row.ToContent()).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home Statistics could not be read safely; rendering approved defaults without writing to the database.");
            return HomeStatisticsDefaults.Content;
        }
    }

    public async Task<HomeStatisticsEditModel> GetForEditAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var rows = await ReadAsync(database, cancellationToken);
        ValidateRows(rows);
        return HomeStatisticsEditModel.FromContent(rows.Select(row => row.ToContent()).ToArray());
    }

    public async Task UpdateAsync(HomeStatisticsEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validator.ValidateObject(model, new ValidationContext(model), true);
        var rows = await database.HomeStatistics.OrderBy(row => row.DisplayOrder).ToListAsync(cancellationToken);
        // Do not insert, delete, repair, or reorder a partial/corrupt collection through an editor save.
        ValidateRows(rows);
        var now = DateTime.UtcNow;
        for (var index = 0; index < rows.Count; index++)
        {
            rows[index].Label = model.Items[index].Label;
            rows[index].Value = model.Items[index].Value!.Value;
            rows[index].UpdatedAtUtc = now;
        }
        // EF wraps the four updates in one transaction.
        await database.SaveChangesAsync(cancellationToken);
    }

    private static Task<List<HomeStatistic>> ReadAsync(ApplicationDbContext database, CancellationToken cancellationToken) =>
        database.HomeStatistics.AsNoTracking().OrderBy(row => row.DisplayOrder).ToListAsync(cancellationToken);

    private static void ValidateRows(IReadOnlyList<HomeStatistic> rows)
    {
        if (rows.Count != HomeStatisticsDefaults.Content.Count ||
            rows.Where((row, index) => row.Id != index + 1 || row.DisplayOrder != index + 1).Any())
            throw new ValidationException("The Home Statistics collection is incomplete or has unexpected order.");
        var model = HomeStatisticsEditModel.FromContent(rows.Select(row => row.ToContent()).ToArray());
        Validator.ValidateObject(model, new ValidationContext(model), true);
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
