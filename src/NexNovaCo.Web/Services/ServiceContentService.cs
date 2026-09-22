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

public sealed class ServiceContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<ServiceContentService> logger) : IServiceContentService
{
    public async Task<IReadOnlyList<ServiceSummary>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await Ordered(database).AsNoTracking().ToListAsync(cancellationToken);
            foreach (var row in rows) Validate(row.ToEditModel());
            // An intentionally empty collection is not a read failure and must not resurrect deleted records.
            return rows.Select(row => row.ToContent()).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Services could not be read safely; rendering approved fallback without database writes.");
            return ServiceCatalog.All;
        }
    }

    public async Task<IReadOnlyList<ServiceListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        return await Ordered(database).AsNoTracking()
            .Select(row => new ServiceListItem(row.Id, row.DisplayOrder, row.Name, row.Tagline, row.IconPath, row.HomeFeatured == null ? null : (int?)row.HomeFeatured.DisplayOrder)).ToListAsync(cancellationToken);
    }

    public async Task<ServiceEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var row = await database.Services.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Service no longer exists.");
        return row.ToEditModel();
    }

    public async Task<int> CreateAsync(ServiceEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        Normalize(rows);
        var row = new ServiceEntity { DisplayOrder = rows.Count + 1 };
        row.SetContent(model);
        database.Services.Add(row);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(int id, ServiceEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        var row = await database.Services.SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Service no longer exists.");
        row.SetContent(model); // Never bind Id/order from the form.
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        var row = rows.SingleOrDefault(row => row.Id == id) ?? throw new KeyNotFoundException("Service no longer exists.");
        database.Services.Remove(row);
        rows.Remove(row);
        Normalize(rows);
        var featured = await database.HomeFeaturedServices.Where(x => x.ServiceId != id).OrderBy(x => x.DisplayOrder).ThenBy(x => x.ServiceId).ToListAsync(cancellationToken);
        for (var i = 0; i < featured.Count; i++) featured[i].DisplayOrder = i + 1;
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

    public async Task<IReadOnlyList<ServiceSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await database.HomeFeaturedServices.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.ServiceId)
                .Select(x => x.Service).ToListAsync(cancellationToken);
            foreach (var row in rows) Validate(row.ToEditModel());
            if (rows.Count > HomeFeaturedServiceRules.MaximumCount) throw new ValidationException("Invalid featured selection.");
            return rows.Select(x => x.ToContent()).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home featured Services could not be read safely; rendering approved fallback without database writes.");
            return ServiceCatalog.HomeFeatured;
        }
    }

    public async Task SaveHomeFeaturedAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        var ids = orderedIds.ToArray();
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        if (ids.Length > HomeFeaturedServiceRules.MaximumCount || ids.Distinct().Count() != ids.Length ||
            await database.Services.CountAsync(x => ids.Contains(x.Id), cancellationToken) != ids.Length)
            throw new ValidationException("Select up to five existing Services without duplicates. Reload if a Service was deleted.");
        var existing = await database.HomeFeaturedServices.ToDictionaryAsync(x => x.ServiceId, cancellationToken);
        database.HomeFeaturedServices.RemoveRange(existing.Values.Where(x => !ids.Contains(x.ServiceId)));
        for (var i = 0; i < ids.Length; i++)
        {
            if (existing.TryGetValue(ids[i], out var relation)) relation.DisplayOrder = i + 1;
            else database.HomeFeaturedServices.Add(new HomeFeaturedService { ServiceId = ids[i], DisplayOrder = i + 1 });
        }
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static IOrderedQueryable<ServiceEntity> Ordered(ApplicationDbContext database) =>
        database.Services.OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id);
    private static void Validate(ServiceEditModel model) =>
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
    private static void Normalize(IReadOnlyList<ServiceEntity> rows)
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
