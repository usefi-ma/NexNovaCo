using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexNovaCo.Web.Components.Pages.Dashboard;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;
using static NexNovaCo.Auth.Tests.AuthChecks;

namespace NexNovaCo.Auth.Tests;

internal static class HomeNavigationChecks
{
    public static async Task RunAsync()
    {
        await using var app = new AuthFactory(NewPassword());
        using var anonymous = app.NewClient();
        using var admin = app.NewClient();
        using var viewer = app.NewClient();
        await Login(admin, AuthFactory.Email, app.Password);
        var viewerPassword = NewPassword();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Check((await users.CreateAsync(new ApplicationUser { UserName = "nav-viewer@example.invalid", Email = "nav-viewer@example.invalid" }, viewerPassword)).Succeeded, "Navigation viewer setup failed.");
        }
        await Login(viewer, "nav-viewer@example.invalid", viewerPassword);
        string[] canonical = ["/dashboard/content/home/hero", "/dashboard/content/home/welcome"];
        string[] aliases = ["/dashboard/content/home", "/dashboard/home/hero"];
        foreach (var route in canonical.Concat(aliases))
        {
            var challenge = await anonymous.GetAsync(route);
            Check(challenge.StatusCode == HttpStatusCode.Redirect && challenge.Headers.Location!.ToString().Contains("/admin/login"), "Every Home CMS route must challenge anonymous users.");
            var denied = await viewer.GetAsync(route);
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("access-denied"), "Every Home CMS route must deny non-Admins.");
        }
        foreach (var route in aliases)
        {
            var redirect = await admin.GetAsync(route);
            Check(redirect.StatusCode == HttpStatusCode.Redirect && redirect.Headers.Location!.ToString().EndsWith(canonical[0]), "Admin alias must redirect directly to canonical Hero.");
        }
        foreach (var route in canonical)
        foreach (var attempt in Enumerable.Range(0, 2))
        {
            var response = await admin.GetAsync(route);
            var html = await response.Content.ReadAsStringAsync();
            Check(response.StatusCode == HttpStatusCode.OK, "Canonical editor must support direct load and refresh.");
            Check(!html.Contains("role=\"tab\"") && html.Contains("mud-nav-group"), "Sidebar group must replace tabs.");
            Check(html.Contains("href=\"" + canonical[0] + "\"") && html.Contains("href=\"" + canonical[1] + "\""), "Home sidebar must link to both canonical editors.");
            Check(route.EndsWith("/hero") ? html.Contains("Opening line") && !html.Contains("Body paragraph 1") : html.Contains("Body paragraph 1") && !html.Contains("Opening line"), "Each page must render only its own form.");
        }
        await CheckEditorStateAsync(new HomeHeroEditor(), "HeroService", new HeroStub(), "OpeningLine");
        await CheckEditorStateAsync(new HomeWelcomeEditor(), "WelcomeService", new WelcomeStub(), "Title");
        await CheckEditorStateAsync(new HomeServicesSectionEditor(), "ServicesService", new ServicesStub(), "Title");
        await CheckEditorStateAsync(new HomeProjectsSectionEditor(), "ProjectsService", new ProjectsStub(), "Title");
        await CheckEditorStateAsync(new HomeTeamSectionEditor(), "TeamService", new TeamStub(), "Title");
        await CheckEditorStateAsync(new HomePartnersSectionEditor(), "PartnersService", new PartnersStub(), "Title");
        await CheckStatisticsEditorAsync();
    }

    private static async Task CheckStatisticsEditorAsync()
    {
        var editor = new HomeStatisticsEditor();
        var service = new StatisticsStub();
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        type.GetProperty("StatisticsService", flags)!.SetValue(editor, service);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<HomeStatisticsEditor>.Instance);
        async Task Invoke(string method) => await (Task)type.GetMethod(method, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        await Invoke("OnInitializedAsync");
        Check(!Dirty(), "Loaded statistics editor is clean.");
        var model = (HomeStatisticsEditModel)type.GetField("_model", flags)!.GetValue(editor)!;
        foreach (var item in model.Items)
        {
            var label = item.Label;
            var value = item.Value;
            item.Label += " changed";
            Check(Dirty(), "Every collection label participates in dirty tracking.");
            item.Label = label;
            Check(!Dirty(), "Reverting a label clears dirty state.");
            item.Value++;
            Check(Dirty(), "Every collection number participates in dirty tracking.");
            item.Value = value;
            Check(!Dirty(), "Reverting a number clears dirty state.");
            item.Value = null;
            Check(Dirty(), "Clearing a required number remains dirty.");
            item.Value = value;
        }
        model.Items[0].Label = "Unsaved";
        type.GetMethod("ClearSaved", flags)!.Invoke(editor, null);
        Check(Dirty(), "Invalid form submission retains collection edits.");
        foreach (var failure in new Exception[] { new DbUpdateException("Technical test detail"), new ValidationException("Technical test detail") })
        {
            service.Failure = failure;
            await Invoke("SaveAsync");
            Check(Dirty() && model.Items[0].Label == "Unsaved", "Failed save preserves collection edits.");
            Check(!((string)type.GetField("_error", flags)!.GetValue(editor)!).Contains("Technical"), "Collection feedback hides technical details.");
        }
        service.Failure = null;
        await Invoke("SaveAsync");
        Check(!Dirty() && (bool)type.GetField("_saved", flags)!.GetValue(editor)!, "Successful collection save is clean with success feedback.");
        model.Items[0].Value++;
        await Invoke("OnInitializedAsync");
        Check(!Dirty(), "Reload establishes a clean collection snapshot.");
    }

    // Invoke the actual editor lifecycle/save methods with a failing service. No UI-only happy-path surrogate.
    private static async Task CheckEditorStateAsync<T>(T editor, string serviceName, Stub service, string fieldName) where T : class
    {
        var type = editor.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        type.GetProperty(serviceName, flags)!.SetValue(editor, service);
        type.GetProperty("Logger", flags)!.SetValue(editor, NullLogger<T>.Instance);
        async Task Invoke(string method) => await (Task)type.GetMethod(method, flags)!.Invoke(editor, null)!;
        bool Dirty() => (bool)type.GetProperty("IsDirty", flags)!.GetValue(editor)!;
        await Invoke("OnInitializedAsync");
        Check(!Dirty(), "Loaded editor must be clean.");
        var model = type.GetField("_model", flags)!.GetValue(editor)!;
        foreach (var property in model.GetType().GetProperties())
        {
            var value = property.GetValue(model);
            property.SetValue(model, value + " changed");
            Check(Dirty(), "Every editable field must participate in the dirty snapshot.");
            property.SetValue(model, value);
            Check(!Dirty(), "Reverting to the saved value must clear dirty state.");
        }
        var field = model.GetType().GetProperty(fieldName)!;
        field.SetValue(model, "Unsaved navigation test");
        type.GetMethod("ClearSaved", flags)!.Invoke(editor, null);
        Check(Dirty(), "Invalid-form submission must not clear dirty state.");
        service.Failure = new DbUpdateException("Simulated test-only failure");
        await Invoke("SaveAsync");
        Check(Dirty() && (string)field.GetValue(model)! == "Unsaved navigation test", "Failed save must preserve input and dirty state.");
        Check(!((string)type.GetField("_error", flags)!.GetValue(editor)!).Contains("Simulated"), "Save error must not expose technical detail.");
        service.Failure = new ValidationException("Simulated validation failure");
        await Invoke("SaveAsync");
        Check(Dirty(), "Service validation failure must retain dirty state.");
        service.Failure = null;
        await Invoke("SaveAsync");
        Check(!Dirty() && (bool)type.GetField("_saved", flags)!.GetValue(editor)!, "Successful save must capture a clean snapshot and show success.");
        field.SetValue(model, "Another unsaved edit");
        await Invoke("OnInitializedAsync");
        Check(!Dirty(), "Reloaded model must reset the snapshot.");
    }

    private abstract class Stub { public Exception? Failure { get; set; } }
    private sealed class PartnersStub : Stub, IHomePartnersSectionContentService
    {
        public Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomePartnersSectionDefaults.Content);
        public Task<HomePartnersSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomePartnersSectionEditModel.FromContent(HomePartnersSectionDefaults.Content));
        public Task UpdateAsync(HomePartnersSectionEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
    private sealed class StatisticsStub : Stub, IHomeStatisticsContentService
    {
        public Task<IReadOnlyList<Statistic>> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeStatisticsDefaults.Content);
        public Task<HomeStatisticsEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeStatisticsEditModel.FromContent(HomeStatisticsDefaults.Content));
        public Task UpdateAsync(HomeStatisticsEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
    private sealed class TeamStub : Stub, IHomeTeamSectionContentService
    {
        public Task<TeamSectionContent> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeTeamSectionDefaults.Content);
        public Task<HomeTeamSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeTeamSectionEditModel.FromContent(HomeTeamSectionDefaults.Content));
        public Task UpdateAsync(HomeTeamSectionEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
    private sealed class ProjectsStub : Stub, IHomeProjectsSectionContentService
    {
        public Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeProjectsSectionDefaults.Content);
        public Task<HomeProjectsSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeProjectsSectionEditModel.FromContent(HomeProjectsSectionDefaults.Content));
        public Task UpdateAsync(HomeProjectsSectionEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
    private sealed class ServicesStub : Stub, IHomeServicesSectionContentService
    {
        public Task<SectionHeading> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeServicesSectionDefaults.Content);
        public Task<HomeServicesSectionEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeServicesSectionEditModel.FromContent(HomeServicesSectionDefaults.Content));
        public Task UpdateAsync(HomeServicesSectionEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
    private sealed class HeroStub : Stub, IHomeHeroContentService
    {
        public Task<HomeHeroContent> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeHeroDefaults.Content);
        public Task<HomeHeroEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeHeroEditModel.FromContent(HomeHeroDefaults.Content));
        public Task UpdateAsync(HomeHeroEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
    private sealed class WelcomeStub : Stub, IHomeWelcomeContentService
    {
        public Task<WelcomeContent> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeWelcomeDefaults.Content);
        public Task<HomeWelcomeEditModel> GetForEditAsync(CancellationToken cancellationToken = default) => Task.FromResult(HomeWelcomeEditModel.FromContent(HomeWelcomeDefaults.Content));
        public Task UpdateAsync(HomeWelcomeEditModel model, CancellationToken cancellationToken = default) => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
}
