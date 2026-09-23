using Microsoft.AspNetCore.Components.Forms;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Instances can only be constructed by the validating storage service; callers cannot mutate bytes.
public sealed class MediaUpload
{
    internal MediaUpload(byte[] bytes, string name, string extension, MediaKind kind)
    { Bytes = bytes; FileName = name; Extension = extension; Kind = kind; }
    internal byte[] Bytes { get; }
    internal string Extension { get; }
    public string FileName { get; }
    public MediaKind Kind { get; }
    public string PreviewUrl => $"data:{MediaPolicy.ContentType("image" + Extension)};base64,{Convert.ToBase64String(Bytes)}";
}

public interface IMediaStorageService
{
    Task<MediaUpload> ReadAsync(IBrowserFile file, MediaKind kind, CancellationToken cancellationToken = default);
    Task<string> SaveAsync(MediaUpload upload, CancellationToken cancellationToken = default);
    void RequireAvailable(string path, MediaKind kind);
    string ResolvePublicPath(string path, MediaKind kind);
}

public sealed class MediaStorageOptions
{
    // Trusted deployment configuration only; never accepted from a form or URL.
    public string? RootPath { get; set; }
}
