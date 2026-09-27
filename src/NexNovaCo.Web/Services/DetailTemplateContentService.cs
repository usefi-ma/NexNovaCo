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

// Template copy only. Entity content, routing and media remain owned by the existing catalogs.
public sealed class DetailTemplateContentService(IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions,
    ILogger<DetailTemplateContentService> logger) : IDetailTemplateContentService
{
    public async Task<ProjectDetailTemplateEditModel> ReadProjectAsync(CancellationToken ct = default)
    {
        try { return await ReadProjectAsync(false, ct); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "Project detail template unavailable; rendering approved defaults without writes.");
            return ProjectDetailTemplateEditModel.Approved();
        }
    }
    public Task<ProjectDetailTemplateEditModel> GetProjectForEditAsync(CancellationToken ct = default) => ReadProjectAsync(true, ct);
    private async Task<ProjectDetailTemplateEditModel> ReadProjectAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.ProjectDetailTemplateSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Project detail template has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveProjectAsync(ProjectDetailTemplateEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.ProjectDetailTemplateSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Project detail template has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MemberDetailTemplateEditModel> ReadMemberAsync(CancellationToken ct = default)
    {
        try { return await ReadMemberAsync(false, ct); }
        catch (Exception exception) when (exception is DbException or ValidationException or KeyNotFoundException)
        {
            logger.LogError(exception, "Member detail template unavailable; rendering approved defaults without writes.");
            return MemberDetailTemplateEditModel.Approved();
        }
    }
    public Task<MemberDetailTemplateEditModel> GetMemberForEditAsync(CancellationToken ct = default) => ReadMemberAsync(true, ct);
    private async Task<MemberDetailTemplateEditModel> ReadMemberAsync(bool admin, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (admin) await RequireAdminAsync(db, ct);
        var row = await db.MemberDetailTemplateSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Member detail template has not been initialized.");
        var model = row.ToEditModel();
        if (!admin) Validate(model);
        return model;
    }
    public async Task SaveMemberAsync(MemberDetailTemplateEditModel model, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireAdminAsync(db, ct);
        Validate(model);
        var row = await db.MemberDetailTemplateSettings.SingleOrDefaultAsync(x => x.Id == 1, ct)
            ?? throw new KeyNotFoundException("Member detail template has not been initialized.");
        row.SetContent(model);
        await db.SaveChangesAsync(ct);
    }

    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);
    private async Task RequireAdminAsync(ApplicationDbContext database, CancellationToken ct)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var claims = identityOptions.Value.ClaimsIdentity;
        var userId = principal.FindFirstValue(claims.UserIdClaimType);
        var stamp = principal.FindFirstValue(claims.SecurityStampClaimType);
        if (principal.Identity?.IsAuthenticated != true || !principal.IsInRole(IdentityDatabaseInitializer.AdminRole) ||
            userId is null || stamp is null ||
            !await database.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.SecurityStamp == stamp, ct) ||
            !await (from membership in database.UserRoles join role in database.Roles on membership.RoleId equals role.Id
                    where membership.UserId == userId && role.Name == IdentityDatabaseInitializer.AdminRole select membership).AnyAsync(ct))
            throw new UnauthorizedAccessException("An active Admin session is required.");
    }
}
