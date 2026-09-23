using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexNovaCo.Web.Components.Pages.Dashboard;
using NexNovaCo.Web.Components.Shared;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class MediaEditorChecks
{
    public static async Task RunAsync(IDbContextFactory<ApplicationDbContext> factory, IMediaStorageService media,
        IHomeHeroContentService hero, IHomeWelcomeContentService welcome, IMemberContentService members,
        IPartnerContentService partners, byte[] jpg, byte[] png, int memberId, int partnerId)
    {
        // Exercise the actual save handlers and actual reusable field, without a fake storage service.
        var cases = new (object Editor, object Model, string ServiceName, object Service, object Logger, MediaKind Kind, string Table, int Id)[]
        {
            (new HomeHeroEditor(), await hero.GetForEditAsync(), "HeroService", hero, NullLogger<HomeHeroEditor>.Instance, MediaKind.Hero, "HomeHeroSettings", 1),
            (new HomeWelcomeEditor(), await welcome.GetForEditAsync(), "WelcomeService", welcome, NullLogger<HomeWelcomeEditor>.Instance, MediaKind.Welcome, "HomeWelcomeSettings", 1),
            (new MemberEditor(), await members.GetForEditAsync(memberId), "MembersService", members, NullLogger<MemberEditor>.Instance, MediaKind.Member, "Members", memberId),
            (new PartnerEditor(), await partners.GetForEditAsync(partnerId), "PartnersService", partners, NullLogger<PartnerEditor>.Instance, MediaKind.Partner, "Partners", partnerId)
        };
        foreach (var item in cases)
        {
            var imageProperty = item.Model.GetType().GetProperty("ImagePath")!;
            var original = (string)imageProperty.GetValue(item.Model)!;
            var field = new CmsImageField();
            SetProperty(field,"Media",media); SetProperty(field,"Value",original); SetProperty(field,"Kind",item.Kind);
            var bytes = item.Kind == MediaKind.Partner ? png : jpg;
            var selected = await media.ReadAsync(new TestFile(bytes,item.Kind == MediaKind.Partner),item.Kind);
            SetField(field,"_pending",selected); SetField(field,"_selectedName",selected.FileName);
            SetField(item.Editor,"_imageField",field); SetField(item.Editor,"_model",item.Model);
            SetProperty(item.Editor,item.ServiceName,item.Service); SetProperty(item.Editor,"Logger",item.Logger);
            if (item.Editor is MemberEditor or PartnerEditor)
            { SetField(item.Editor,"_editingId",(int?)item.Id); SetProperty(item.Editor,"Id",(int?)item.Id); }
            var snapshot = (EditorSnapshot)GetField(item.Editor,"_snapshot")!;
            snapshot.Capture((string[])GetProperty(item.Editor,"CurrentValues")!);
            Check((bool)GetProperty(item.Editor,"IsDirty")!, "Pending image alone activates unsaved-changes guard.");

            await using (var db = await factory.CreateDbContextAsync())
            {
                var create = "CREATE TRIGGER BlockImageEditor BEFORE UPDATE ON " + item.Table + " BEGIN SELECT RAISE(ABORT, 'isolated image save failure'); END;";
                await db.Database.ExecuteSqlRawAsync(create);
            }
            await InvokeSaveAsync(item.Editor);
            Check(!(bool)GetField(item.Editor,"_saved")! && GetField(item.Editor,"_error") is string, "Actual editor reports failed entity save.");
            Check((string)imageProperty.GetValue(item.Model)! == original && field.HasPendingSelection, "Failed form save restores prior model path and retains the pending replacement.");
            var cached = (string)GetField(field,"_storedPath")!;
            Check(MediaPolicy.IsGenerated(cached,item.Kind), "Failed editor save retains safely persisted file for retry.");

            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockImageEditor;");
            await InvokeSaveAsync(item.Editor);
            Check((bool)GetField(item.Editor,"_saved")! && GetField(item.Editor,"_error") is null, "Actual editor retry succeeds.");
            Check((string)imageProperty.GetValue(item.Model)! == cached && !field.HasPendingSelection && !(bool)GetProperty(item.Editor,"IsDirty")!, "Retry reuses stored path and clears upload/dirty state only after DB success.");
            field.Dispose();
        }
    }
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static void SetField(object target,string name,object? value) => target.GetType().GetField(name,Flags)!.SetValue(target,value);
    private static object? GetField(object target,string name) => target.GetType().GetField(name,Flags)!.GetValue(target);
    private static void SetProperty(object target,string name,object? value) => target.GetType().GetProperty(name,Flags)!.SetValue(target,value);
    private static object? GetProperty(object target,string name) => target.GetType().GetProperty(name,Flags)!.GetValue(target);
    private static Task InvokeSaveAsync(object editor) => (Task)editor.GetType().GetMethod("SaveAsync",Flags)!.Invoke(editor,null)!;
    private sealed class TestFile(byte[] bytes,bool png) : IBrowserFile
    {
        public string Name => png ? "editor.png" : "editor.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => bytes.Length;
        public string ContentType => png ? "image/png" : "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000,CancellationToken cancellationToken = default) => new MemoryStream(bytes,false);
    }
}
