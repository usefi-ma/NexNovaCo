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

public sealed class NavigationContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<NavigationContentService> logger) : INavigationContentService
{
    public async Task<IReadOnlyList<NavigationListItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await Ordered(database).AsNoTracking().ToListAsync(cancellationToken);
            foreach (var row in rows) Validate(row.ToEditModel());
            // An intentionally empty collection is not a read failure and must not resurrect deleted records.
            return rows.Select(row => new NavigationListItem(row.Id, row.DisplayOrder, row.Label, row.Url)).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException or InvalidOperationException)
        {
            logger.LogError(exception, "Navigations could not be read safely; rendering approved fallback without database writes.");
            return GlobalSiteDefaults.Navigation;
        }
    }

    public async Task<IReadOnlyList<NavigationListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        return await Ordered(database).AsNoTracking()
            .Select(row => new NavigationListItem(row.Id, row.DisplayOrder, row.Label, row.Url)).ToListAsync(cancellationToken);
    }

    public async Task<NavigationEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var row = await database.NavigationItems.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Navigation no longer exists.");
        return row.ToEditModel();
    }

    public async Task<int> CreateAsync(NavigationEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        Normalize(rows);
        var row = new NavigationItem { DisplayOrder = rows.Count + 1 };
        row.SetContent(model);
        database.NavigationItems.Add(row);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(int id, NavigationEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        var row = await database.NavigationItems.SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Navigation no longer exists.");
        row.SetContent(model); // Never bind Id/order from the form.
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        var row = rows.SingleOrDefault(row => row.Id == id) ?? throw new KeyNotFoundException("Navigation no longer exists.");
        database.NavigationItems.Remove(row);
        rows.Remove(row);
        Normalize(rows);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        var ids = orderedIds.ToArray();
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length ||
            !ids.Order().SequenceEqual(rows.Select(row => row.Id).Order()))
            throw new ValidationException("The collection changed. Reload the list before reordering.");
        var byId = rows.ToDictionary(row => row.Id);
        Normalize(ids.Select(id => byId[id]).ToList());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static IOrderedQueryable<NavigationItem> Ordered(ApplicationDbContext database) =>
        database.NavigationItems.OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id);
    private static void Validate(NavigationEditModel model) =>
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
    private static void Normalize(IReadOnlyList<NavigationItem> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            if (rows[index].DisplayOrder == index + 1) continue;
            rows[index].DisplayOrder = index + 1;
            rows[index].UpdatedAtUtc = DateTime.UtcNow;
        }
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
