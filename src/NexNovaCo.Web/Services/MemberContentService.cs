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

public sealed class MemberContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<MemberContentService> logger, MemberCatalog defaults, IMediaStorageService? media = null) : IMemberContentService
{
    public async Task<IReadOnlyList<TeamMemberSummary>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await Ordered(database).AsNoTracking().ToListAsync(cancellationToken);
            foreach (var row in rows) Validate(row.ToEditModel());
            // An intentionally empty collection is not a read failure and must not resurrect deleted records.
            return rows.Select(row => row.ToContent() with { ImagePath = MediaAvailability.Resolve(media, row.ImagePath, MediaKind.Member) }).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Members could not be read safely; rendering approved fallback without database writes.");
            return await defaults.GetAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<MemberListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        return await Ordered(database).AsNoTracking()
            .Select(row => new MemberListItem(row.Id, row.DisplayOrder, row.Name, row.Slug, row.Role, row.ImagePath, row.HomeFeatured == null ? null : (int?)row.HomeFeatured.DisplayOrder)).ToListAsync(cancellationToken);
    }

    public async Task<MemberEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var row = await database.Members.Include(x => x.Skills).AsSingleQuery().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Member no longer exists.");
        return row.ToEditModel();
    }

    public async Task<int> CreateAsync(MemberEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        MediaAvailability.Require(media, model.ImagePath, MediaKind.Member);
        await RequireUniqueSlugAsync(database, model.Slug, 0, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        Normalize(rows);
        var row = new MemberEntity { DisplayOrder = rows.Count + 1 };
        row.SetContent(model);
        database.Members.Add(row);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(int id, MemberEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        MediaAvailability.Require(media, model.ImagePath, MediaKind.Member);
        await RequireUniqueSlugAsync(database, model.Slug, id, cancellationToken);
        var row = await database.Members.Include(x => x.Skills).AsSingleQuery().SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Member no longer exists.");
        row.SetContent(model); // Never bind Id/order from the form.
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        var row = rows.SingleOrDefault(row => row.Id == id) ?? throw new KeyNotFoundException("Member no longer exists.");
        database.Members.Remove(row);
        rows.Remove(row);
        Normalize(rows);
        var featured = await database.HomeFeaturedMembers.Where(x => x.MemberId != id).OrderBy(x => x.DisplayOrder).ThenBy(x => x.MemberId).ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyList<TeamMemberSummary>> GetHomeFeaturedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await database.Members.AsNoTracking().Where(x => x.HomeFeatured != null)
                .Include(x => x.Skills).AsSingleQuery()
                .OrderBy(x => x.HomeFeatured!.DisplayOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
            foreach (var row in rows) Validate(row.ToEditModel());
            return rows.Select(x => x.ToContent() with { ImagePath = MediaAvailability.Resolve(media, x.ImagePath, MediaKind.Member) }).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Home featured Members could not be read safely; rendering approved fallback without database writes.");
            return await defaults.GetHomeFeaturedAsync(cancellationToken);
        }
    }

    public async Task SaveHomeFeaturedAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        var ids = orderedIds.ToArray();
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        if (ids.Distinct().Count() != ids.Length ||
            await database.Members.CountAsync(x => ids.Contains(x.Id), cancellationToken) != ids.Length)
            throw new ValidationException("Select existing Members without duplicates. Reload if a Member was deleted.");
        var existing = await database.HomeFeaturedMembers.ToDictionaryAsync(x => x.MemberId, cancellationToken);
        database.HomeFeaturedMembers.RemoveRange(existing.Values.Where(x => !ids.Contains(x.MemberId)));
        for (var i = 0; i < ids.Length; i++)
        {
            if (existing.TryGetValue(ids[i], out var relation)) relation.DisplayOrder = i + 1;
            else database.HomeFeaturedMembers.Add(new HomeFeaturedMember { MemberId = ids[i], DisplayOrder = i + 1 });
        }
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<MemberDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default)
    {
        // Public URLs retain exact canonical slugs; malformed/unknown routes are not fallback failures.
        if (!MemberSlugs.IsValid(slug)) return null;
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var row = await database.Members.AsNoTracking().Include(x => x.Skills).AsSingleQuery()
                .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
            if (row is null) return null;
            Validate(row.ToEditModel());
            var detail = row.ToDetail();
            return detail with { Summary = detail.Summary with { ImagePath = MediaAvailability.Resolve(media, row.ImagePath, MediaKind.Member) } };
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Member detail could not be read safely; rendering approved fallback without writes.");
            return await defaults.GetDetailAsync(slug, cancellationToken);
        }
    }

    private static async Task RequireUniqueSlugAsync(ApplicationDbContext database, string slug, int exceptId, CancellationToken cancellationToken)
    {
        var normalized = MemberSlugs.Normalize(slug);
        if (await database.Members.AnyAsync(x => x.Id != exceptId && x.Slug == normalized, cancellationToken))
            throw new ValidationException("That member slug is already in use. Choose another slug.");
    }

    private static IOrderedQueryable<MemberEntity> Ordered(ApplicationDbContext database) =>
        database.Members.Include(x => x.Skills).AsSingleQuery().OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id);
    private static void Validate(MemberEditModel model) =>
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
    private static void Normalize(IReadOnlyList<MemberEntity> rows)
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
