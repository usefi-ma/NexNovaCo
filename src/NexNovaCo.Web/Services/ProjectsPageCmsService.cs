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

// Page settings only; shared Projects and Testimonials remain in their canonical catalog services.
public sealed class ProjectsPageCmsService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    IMediaStorageService media, ILogger<ProjectsPageCmsService> logger) : IProjectsPageCmsService
{
    public async Task<ProjectsContent> ReadPublicAsync(CancellationToken ct = default)
    {
        var hero = await SafeReadAsync(() => ReadHeroAsync(false, ct), ProjectsHeroEditModel.Approved, "Hero");
        var testimonials = await SafeReadAsync(() => ReadTestimonialsAsync(false, ct), ProjectsTestimonialsEditModel.Approved, "Testimonials");
        return new(new(hero.Title, hero.MobileTitle, hero.Description, hero.CtaLabel, hero.CtaHref),
            [], [], new(testimonials.Title, testimonials.Description),
            media.ResolvePublicPath(hero.ImagePath, MediaKind.ProjectsHero));
    }
    private async Task<T> SafeReadAsync<T>(Func<Task<T>> read, Func<T> fallback, string section)
    {
        try { return await read(); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "Projects {Section} could not be read; rendering approved defaults without writes.", section);
            return fallback();
        }
    }
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);

    public Task<ProjectsHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default) => ReadHeroAsync(true, ct);
    private async Task<ProjectsHeroEditModel> ReadHeroAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ProjectsHeroSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Projects Hero has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveHeroAsync(ProjectsHeroEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        media.RequireAvailable(model.ImagePath, MediaKind.ProjectsHero);
        var row = await db.ProjectsHeroSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Projects Hero has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public Task<ProjectsTestimonialsEditModel> GetTestimonialsForEditAsync(CancellationToken ct = default) => ReadTestimonialsAsync(true, ct);
    private async Task<ProjectsTestimonialsEditModel> ReadTestimonialsAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ProjectsTestimonialsSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Projects Testimonials has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveTestimonialsAsync(ProjectsTestimonialsEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ProjectsTestimonialsSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Projects Testimonials has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
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
