using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public sealed class LocalMediaStorageService(MediaFilePaths paths, IDbContextFactory<ApplicationDbContext> factory,
    AuthenticationStateProvider authentication, IOptions<IdentityOptions> identityOptions) : IMediaStorageService
{
    public async Task<MediaUpload> ReadAsync(IBrowserFile file, MediaKind kind, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        var limit = MediaPolicy.MaxBytes(kind);
        var name = file.Name;
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || name.Any(char.IsControl) ||
            name.IndexOfAny(['/', '\\', ':', '<', '>', '"']) >= 0 || name.Contains(".."))
            throw new ValidationException("Use a plain image filename without paths.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (extension == ".jpeg") extension = ".jpg";
        if (extension is not (".jpg" or ".png" or ".webp") || file.ContentType != MediaPolicy.ContentType("image" + extension))
            throw new ValidationException("Choose a JPG, PNG or WebP image. SVG uploads are not supported.");
        if (file.Size <= 0 || file.Size > limit) throw new ValidationException($"Choose an image up to {limit / 1024 / 1024} MB.");
        await using var input = file.OpenReadStream(limit, cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (output.Length + read > limit) throw new ValidationException("The image exceeds the upload limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        var bytes = output.ToArray();
        if (bytes.Length != file.Size) throw new ValidationException("The image size changed. Select it again.");
        ImageSignatureValidator.Validate(bytes, extension);
        return new MediaUpload(bytes, name, extension, kind);
    }

    public async Task<string> SaveAsync(MediaUpload upload, CancellationToken cancellationToken = default)
    {
        // Selection is not authorization to save later: recheck the live role and security stamp.
        await RequireAdminAsync(cancellationToken);
        if (upload.Bytes.Length > MediaPolicy.MaxBytes(upload.Kind)) throw new ValidationException("The image exceeds the upload limit.");
        ImageSignatureValidator.Validate(upload.Bytes, upload.Extension);
        var relative = $"uploads/{MediaPolicy.Folder(upload.Kind)}/{Guid.NewGuid():N}{upload.Extension}";
        var destination = paths.PhysicalPath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        destination = paths.PhysicalPath(relative);
        var temporary = destination + ".tmp";
        var created = false;
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
            {
                created = true;
                await stream.WriteAsync(upload.Bytes, cancellationToken);
            }
            File.Move(temporary, destination, overwrite: false);
        }
        finally
        {
            // Only our own incomplete temporary file; never delete a previous image or a bundled asset.
            if (created && File.Exists(temporary)) File.Delete(temporary);
        }
        return relative;
    }

    public void RequireAvailable(string path, MediaKind kind)
    {
        if (!MediaPolicy.IsAllowed(path, kind) || (MediaPolicy.IsGenerated(path) && !paths.Exists(path)))
            throw new ValidationException("The selected image is unavailable. Choose another image.");
    }

    public string ResolvePublicPath(string path, MediaKind kind) =>
        MediaPolicy.IsAllowed(path, kind) && (!MediaPolicy.IsGenerated(path) || paths.Exists(path))
            ? path : MediaPolicy.Fallback(kind);

    private async Task RequireAdminAsync(CancellationToken cancellationToken)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var claims = identityOptions.Value.ClaimsIdentity;
        var userId = principal.FindFirstValue(claims.UserIdClaimType);
        var stamp = principal.FindFirstValue(claims.SecurityStampClaimType);
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        if (principal.Identity?.IsAuthenticated != true || !principal.IsInRole(IdentityDatabaseInitializer.AdminRole) ||
            userId is null || stamp is null ||
            !await database.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.SecurityStamp == stamp, cancellationToken) ||
            !await (from membership in database.UserRoles join role in database.Roles on membership.RoleId equals role.Id
                    where membership.UserId == userId && role.Name == IdentityDatabaseInitializer.AdminRole select membership).AnyAsync(cancellationToken))
            throw new UnauthorizedAccessException("An active Admin session is required.");
    }
}
