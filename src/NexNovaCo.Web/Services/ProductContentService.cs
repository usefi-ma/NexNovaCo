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

public sealed class ProductContentService(
    IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication,
    IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media,
    ILogger<ProductContentService> logger) : IProductContentService
{
    public async Task<IReadOnlyList<ProductSummary>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var rows = await Ordered(database).AsNoTracking().ToListAsync(cancellationToken);
            foreach (var row in rows) Validate(row.ToEditModel());
            return rows.Select(row => ResolveImage(row.ToContent())).ToArray();
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            logger.LogError(exception, "Products could not be read safely; rendering approved fallback without database writes.");
            return ShopDefaults.Products.Select(ResolveImage).ToArray();
        }
    }

    public async Task<ProductDetail?> GetDetailAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = ProductSlugs.Normalize(slug);
        if (!ProductSlugs.IsValid(normalized)) return null;
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var row = await database.Products.AsNoTracking()
                .Include(x => x.Gallery)
                .Include(x => x.Features)
                .Include(x => x.RelatedProducts).ThenInclude(x => x.RelatedProduct)
                .AsSingleQuery()
                .SingleOrDefaultAsync(x => x.Slug == normalized, cancellationToken);
            if (row is null) return null;
            Validate(row.ToEditModel());
            return new ProductDetail(
                ResolveImage(row.ToContent()),
                row.FullDescription,
                row.Gallery.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
                    .Select(x => new ProductImage(media.ResolvePublicPath(x.Source, MediaKind.Product), x.Alt)).ToArray(),
                row.Features.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => x.Text).ToArray(),
                row.RelatedProducts.OrderBy(x => x.DisplayOrder).ThenBy(x => x.RelatedProductId).Take(3)
                    .Select(x => ResolveImage(x.RelatedProduct.ToContent())).ToArray());
        }
        catch (Exception exception) when (exception is DbException or ValidationException)
        {
            // A detail lookup must never resurrect a deleted Product from defaults.
            logger.LogError(exception, "Product detail {ProductSlug} could not be read safely.", normalized);
            return null;
        }
    }

    public async Task<IReadOnlyList<ProductListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        return await Ordered(database).AsNoTracking().Select(row => new ProductListItem(
            row.Id, row.DisplayOrder, row.Name, row.Slug, row.Price, row.Badge, row.ImagePath, row.UpdatedAtUtc)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSelectionItem>> ListChoicesAsync(int? exceptId = null, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        return await Ordered(database).AsNoTracking().Where(x => x.Id != exceptId)
            .Select(x => new ProductSelectionItem(x.Id, x.Name, x.Slug)).ToListAsync(cancellationToken);
    }

    public async Task<ProductEditModel> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        return (await database.Products.AsNoTracking()
            .Include(x => x.Gallery).Include(x => x.Features).Include(x => x.RelatedProducts)
            .AsSingleQuery().SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Product no longer exists.")).ToEditModel();
    }

    public async Task<int> CreateAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        RequireMedia(model);
        await RequireRelatedProductsAsync(database, model, 0, cancellationToken);
        await RequireUniqueSlugAsync(database, model.Slug, 0, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        Normalize(rows);
        var row = new ProductEntity { DisplayOrder = rows.Count + 1 };
        row.SetContent(model);
        database.Products.Add(row);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(int id, ProductEditModel model, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        Validate(model);
        RequireMedia(model);
        await RequireRelatedProductsAsync(database, model, id, cancellationToken);
        await RequireUniqueSlugAsync(database, model.Slug, id, cancellationToken);
        var row = await database.Products
            .Include(x => x.Gallery).Include(x => x.Features).Include(x => x.RelatedProducts)
            .AsSingleQuery().SingleOrDefaultAsync(row => row.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Product no longer exists.");
        row.SetContent(model);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await RequireAdminAsync(database, cancellationToken);
        var rows = await Ordered(database).ToListAsync(cancellationToken);
        var row = rows.SingleOrDefault(row => row.Id == id) ?? throw new KeyNotFoundException("Product no longer exists.");
        database.Products.Remove(row);
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
        Normalize(ids.Select(id => byId[id]).ToArray());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private ProductSummary ResolveImage(ProductSummary product) => product with
    {
        ImagePath = media.ResolvePublicPath(product.ImagePath, MediaKind.Product)
    };

    private static IOrderedQueryable<ProductEntity> Ordered(ApplicationDbContext database) =>
        database.Products.OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id);

    private static void Validate(ProductEditModel model) =>
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);

    private static async Task RequireUniqueSlugAsync(ApplicationDbContext database, string slug, int exceptId, CancellationToken cancellationToken)
    {
        var normalized = ProductSlugs.Normalize(slug);
        if (await database.Products.AnyAsync(row => row.Id != exceptId && row.Slug == normalized, cancellationToken))
            throw new ValidationException("That product slug is already in use. Choose another slug.");
    }

    private static async Task RequireRelatedProductsAsync(ApplicationDbContext database, ProductEditModel model, int productId,
        CancellationToken cancellationToken)
    {
        if (productId > 0 && model.RelatedProductIds.Contains(productId))
            throw new ValidationException("A product cannot be related to itself.");
        var ids = model.RelatedProductIds.Distinct().ToArray();
        if (ids.Length == 0) return;
        var existing = await database.Products.AsNoTracking().CountAsync(x => ids.Contains(x.Id), cancellationToken);
        if (existing != ids.Length) throw new ValidationException("One or more related products no longer exist. Reload and try again.");
    }

    private void RequireMedia(ProductEditModel model)
    {
        media.RequireAvailable(model.ImagePath, MediaKind.Product);
        foreach (var image in model.Gallery) media.RequireAvailable(image.Source, MediaKind.Product);
    }

    private static void Normalize(IReadOnlyList<ProductEntity> rows)
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
