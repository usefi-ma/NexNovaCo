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

// Services-page content only; shared Service entities remain canonical in IServiceContentService.
public sealed class ServicesPageCmsService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media, ILogger<ServicesPageCmsService> logger) : IServicesPageCmsService
{
    public async Task<ServicesContent> ReadPublicAsync(CancellationToken ct = default)
    {
        var hero = await SafeReadAsync(() => ReadHeroAsync(false, ct), ServicesHeroEditModel.Approved, "Hero");
        var benefits = await SafeReadAsync(() => ReadBenefitsAsync(false, ct), ServicesBenefitsEditModel.Approved, "Benefits");
        var process = await SafeReadAsync(() => ReadProcessAsync(false, ct), ServicesProcessEditModel.Approved, "Process");
        var pricing = await SafeReadAsync(() => ReadPricingAsync(false, ct), ServicesPricingEditModel.Approved, "Pricing");
        var faq = await SafeReadAsync(() => ReadFaqAsync(false, ct), ServicesFaqEditModel.Approved, "Faq");
        var benefitsItems = await SafeReadAsync<IReadOnlyList<BenefitContent>>(async () => (await ReadBenefitItemsAsync(false, ct)).Select(x => x.ToContent()).ToArray(), () => ServicesPageDefaults.Content.Benefits, "benefits");
        var processItems = await SafeReadAsync<IReadOnlyList<ProcessStepContent>>(async () => (await ReadProcessItemsAsync(false, ct)).Select(x => x.ToContent()).ToArray(), () => ServicesPageDefaults.Content.Steps, "process");
        var pricingItems = await SafeReadAsync<IReadOnlyList<PricingPlan>>(async () => (await ReadPricingItemsAsync(false, ct)).Select(x => x.ToContent()).ToArray(), () => ServicesPageDefaults.Content.Plans, "pricing");
        var faqItems = await SafeReadAsync<IReadOnlyList<FaqContent>>(async () => (await ReadFaqItemsAsync(false, ct)).Select(x => x.ToContent()).ToArray(), () => ServicesPageDefaults.Content.Questions, "faq");
        return new(new(hero.Title, hero.MobileTitle, hero.Description, hero.CtaLabel, hero.CtaHref), [],
            new(benefits.Title, benefits.Description), benefitsItems,
            new(process.Title, process.Description), processItems,
            new(pricing.Title, pricing.Description), pricingItems,
            new(faq.Title, faq.Description), faqItems,
            media.ResolvePublicPath(hero.ImagePath, MediaKind.ServicesHero),
            media.ResolvePublicPath(benefits.ImagePath, MediaKind.ServicesBenefits));
    }
    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "Services {Section} could not be read; rendering approved defaults without writes.", section);
            return fallback();
        }
    }
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);

    public Task<ServicesHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default) => ReadHeroAsync(true, ct);
    private async Task<ServicesHeroEditModel> ReadHeroAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ServicesHeroSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Hero has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveHeroAsync(ServicesHeroEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.ServicesHero);
        var row = await db.ServicesHeroSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Hero has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ServicesBenefitsEditModel> GetBenefitsForEditAsync(CancellationToken ct = default) => ReadBenefitsAsync(true, ct);
    private async Task<ServicesBenefitsEditModel> ReadBenefitsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ServicesBenefitsSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Benefits has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveBenefitsAsync(ServicesBenefitsEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.ServicesBenefits);
        var row = await db.ServicesBenefitsSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Benefits has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ServicesProcessEditModel> GetProcessForEditAsync(CancellationToken ct = default) => ReadProcessAsync(true, ct);
    private async Task<ServicesProcessEditModel> ReadProcessAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ServicesProcessSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Process has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveProcessAsync(ServicesProcessEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ServicesProcessSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Process has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ServicesPricingEditModel> GetPricingForEditAsync(CancellationToken ct = default) => ReadPricingAsync(true, ct);
    private async Task<ServicesPricingEditModel> ReadPricingAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ServicesPricingSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Pricing has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SavePricingAsync(ServicesPricingEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ServicesPricingSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Pricing has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ServicesFaqEditModel> GetFaqForEditAsync(CancellationToken ct = default) => ReadFaqAsync(true, ct);
    private async Task<ServicesFaqEditModel> ReadFaqAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ServicesFaqSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Faq has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveFaqAsync(ServicesFaqEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ServicesFaqSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Services Faq has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<ServiceBenefitItem>> ListBenefitAsync(CancellationToken ct = default) => ReadBenefitItemsAsync(true, ct);
    private async Task<IReadOnlyList<ServiceBenefitItem>> ReadBenefitItemsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var rows = await db.ServiceBenefits.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (!admin) foreach (var row in rows) Validate(row.ToEditModel());
        return rows;
    }
    public async Task<ServiceBenefitEditModel> GetBenefitItemForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        return (await db.ServiceBenefits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.")).ToEditModel();
    }
    public async Task<int> CreateBenefitAsync(ServiceBenefitEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var rows = await db.ServiceBenefits.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].DisplayOrder = i + 1;
        var row = new ServiceBenefitItem { DisplayOrder = rows.Count + 1 }; row.SetContent(model);
        db.ServiceBenefits.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return row.Id;
    }
    public async Task UpdateBenefitAsync(int id, ServiceBenefitEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var row = await db.ServiceBenefits.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.");
        row.SetContent(model); await db.SaveChangesAsync(ct);
    }
    public async Task DeleteBenefitAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServiceBenefits.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("The item no longer exists.");
        db.ServiceBenefits.Remove(row); rows.Remove(row);
        for (var i = 0; i < rows.Count; i++) { rows[i].DisplayOrder = i + 1; rows[i].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task ReorderBenefitAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServiceBenefits.ToDictionaryAsync(x => x.Id, ct);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(rows.Keys.Order()))
            throw new ValidationException("The collection changed. Refresh before reordering.");
        for (var i = 0; i < ids.Length; i++) { rows[ids[i]].DisplayOrder = i + 1; rows[ids[i]].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public Task<IReadOnlyList<ServiceProcessStepItem>> ListProcessAsync(CancellationToken ct = default) => ReadProcessItemsAsync(true, ct);
    private async Task<IReadOnlyList<ServiceProcessStepItem>> ReadProcessItemsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var rows = await db.ServiceProcessSteps.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (!admin) foreach (var row in rows) Validate(row.ToEditModel());
        return rows;
    }
    public async Task<ServiceProcessEditModel> GetProcessItemForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        return (await db.ServiceProcessSteps.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.")).ToEditModel();
    }
    public async Task<int> CreateProcessAsync(ServiceProcessEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var rows = await db.ServiceProcessSteps.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].DisplayOrder = i + 1;
        var row = new ServiceProcessStepItem { DisplayOrder = rows.Count + 1 }; row.SetContent(model);
        db.ServiceProcessSteps.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return row.Id;
    }
    public async Task UpdateProcessAsync(int id, ServiceProcessEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var row = await db.ServiceProcessSteps.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.");
        row.SetContent(model); await db.SaveChangesAsync(ct);
    }
    public async Task DeleteProcessAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServiceProcessSteps.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("The item no longer exists.");
        db.ServiceProcessSteps.Remove(row); rows.Remove(row);
        for (var i = 0; i < rows.Count; i++) { rows[i].DisplayOrder = i + 1; rows[i].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task ReorderProcessAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServiceProcessSteps.ToDictionaryAsync(x => x.Id, ct);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(rows.Keys.Order()))
            throw new ValidationException("The collection changed. Refresh before reordering.");
        for (var i = 0; i < ids.Length; i++) { rows[ids[i]].DisplayOrder = i + 1; rows[ids[i]].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public Task<IReadOnlyList<ServicePricingPlanItem>> ListPricingAsync(CancellationToken ct = default) => ReadPricingItemsAsync(true, ct);
    private async Task<IReadOnlyList<ServicePricingPlanItem>> ReadPricingItemsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var rows = await db.ServicePricingPlans.AsNoTracking().Include(x => x.Features).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (!admin) foreach (var row in rows) Validate(row.ToEditModel());
        return rows;
    }
    public async Task<ServicePricingEditModel> GetPricingItemForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        return (await db.ServicePricingPlans.AsNoTracking().Include(x => x.Features).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.")).ToEditModel();
    }
    public async Task<int> CreatePricingAsync(ServicePricingEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        await RequireAvailableTreatmentAsync(db, model, null, ct);
        var rows = await db.ServicePricingPlans.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].DisplayOrder = i + 1;
        var row = new ServicePricingPlanItem { DisplayOrder = rows.Count + 1 }; row.SetContent(model);
        db.ServicePricingPlans.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return row.Id;
    }
    public async Task UpdatePricingAsync(int id, ServicePricingEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        await RequireAvailableTreatmentAsync(db, model, id, ct);
        var row = await db.ServicePricingPlans.Include(x => x.Features).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.");
        row.SetContent(model); await db.SaveChangesAsync(ct);
    }
    public async Task DeletePricingAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServicePricingPlans.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("The item no longer exists.");
        db.ServicePricingPlans.Remove(row); rows.Remove(row);
        for (var i = 0; i < rows.Count; i++) { rows[i].DisplayOrder = i + 1; rows[i].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task ReorderPricingAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServicePricingPlans.ToDictionaryAsync(x => x.Id, ct);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(rows.Keys.Order()))
            throw new ValidationException("The collection changed. Refresh before reordering.");
        for (var i = 0; i < ids.Length; i++) { rows[ids[i]].DisplayOrder = i + 1; rows[ids[i]].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public Task<IReadOnlyList<ServiceFaqItem>> ListFaqAsync(CancellationToken ct = default) => ReadFaqItemsAsync(true, ct);
    private async Task<IReadOnlyList<ServiceFaqItem>> ReadFaqItemsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var rows = await db.ServiceFaqItems.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (!admin) foreach (var row in rows) Validate(row.ToEditModel());
        return rows;
    }
    public async Task<ServiceFaqEditModel> GetFaqItemForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        return (await db.ServiceFaqItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.")).ToEditModel();
    }
    public async Task<int> CreateFaqAsync(ServiceFaqEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var rows = await db.ServiceFaqItems.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].DisplayOrder = i + 1;
        var row = new ServiceFaqItem { DisplayOrder = rows.Count + 1 }; row.SetContent(model);
        db.ServiceFaqItems.Add(row); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return row.Id;
    }
    public async Task UpdateFaqAsync(int id, ServiceFaqEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct); Validate(model);
        var row = await db.ServiceFaqItems.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The item no longer exists.");
        row.SetContent(model); await db.SaveChangesAsync(ct);
    }
    public async Task DeleteFaqAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServiceFaqItems.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var row = rows.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("The item no longer exists.");
        db.ServiceFaqItems.Remove(row); rows.Remove(row);
        for (var i = 0; i < rows.Count; i++) { rows[i].DisplayOrder = i + 1; rows[i].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task ReorderFaqAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await RequireAdminAsync(db, ct);
        var rows = await db.ServiceFaqItems.ToDictionaryAsync(x => x.Id, ct);
        if (ids.Length != rows.Count || ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(rows.Keys.Order()))
            throw new ValidationException("The collection changed. Refresh before reordering.");
        for (var i = 0; i < ids.Length; i++) { rows[ids[i]].DisplayOrder = i + 1; rows[ids[i]].UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    private static async Task RequireAvailableTreatmentAsync(ApplicationDbContext db, ServicePricingEditModel model, int? id, CancellationToken ct)
    {
        if (model.Treatment == PricingTreatment.Premium && await db.ServicePricingPlans.AnyAsync(x => x.Treatment == PricingTreatment.Premium && x.Id != id, ct))
            throw new ValidationException("Only one plan can use the highlighted Premium treatment. Change the existing highlighted plan first.");
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
