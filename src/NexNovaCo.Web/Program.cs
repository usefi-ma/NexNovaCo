using NexNovaCo.Web.Components;

using MudBlazor.Services;
using NexNovaCo.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Components.Account;
using NexNovaCo.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddSingleton<PublicSiteUrls>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always);

var identityConnection = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("IdentityConnection")
        ?? throw new InvalidOperationException("IdentityConnection is not configured."));
if (!Path.IsPathRooted(identityConnection.DataSource))
    identityConnection.DataSource = Path.GetFullPath(identityConnection.DataSource, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(identityConnection.DataSource)!);
// The factory also registers a scoped context for Identity's normal HTTP/scoped stores.
// CMS operations create/dispose their own context instead of retaining one for a circuit.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(identityConnection.ConnectionString));
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedAccount = false; // No public registration or email workflows in this phase.
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
}).AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<ApplicationDbContext>()
  .AddSignInManager()
  .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "NexNovaCo.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.AddSingleton<ProjectCatalog>();
builder.Services.AddScoped<IProjectContentService, ProjectContentService>();
builder.Services.AddScoped<IProjectCatalog>(services => services.GetRequiredService<IProjectContentService>());
builder.Services.AddSingleton<MemberCatalog>();
builder.Services.AddScoped<IMemberContentService, MemberContentService>();
builder.Services.AddScoped<IMemberCatalog>(services => services.GetRequiredService<IMemberContentService>());
builder.Services.AddScoped<ITeamContentService, TeamContentService>();
builder.Services.AddScoped<IContactContentService, ContactContentService>();
builder.Services.AddScoped<IContactPageCmsService, ContactPageCmsService>();
builder.Services.AddScoped<IGlobalSettingsService, GlobalSettingsService>();
builder.Services.AddScoped<INavigationContentService, NavigationContentService>();
builder.Services.AddScoped<ISocialLinkContentService, SocialLinkContentService>();
builder.Services.AddSingleton<IContactFormService, DemoContactFormService>();
builder.Services.AddScoped<IHomeHeroContentService, HomeHeroContentService>();
builder.Services.AddScoped<IHomeWelcomeContentService, HomeWelcomeContentService>();
builder.Services.AddScoped<IHomeServicesSectionContentService, HomeServicesSectionContentService>();
builder.Services.AddScoped<IHomeProjectsSectionContentService, HomeProjectsSectionContentService>();
builder.Services.AddScoped<IHomeTeamSectionContentService, HomeTeamSectionContentService>();
builder.Services.AddScoped<IHomeStatisticsContentService, HomeStatisticsContentService>();
builder.Services.AddScoped<IHomePartnersSectionContentService, HomePartnersSectionContentService>();
builder.Services.AddScoped<IHomeTestimonialsSectionContentService, HomeTestimonialsSectionContentService>();
builder.Services.AddScoped<IPartnerContentService, PartnerContentService>();
builder.Services.AddScoped<ITestimonialContentService, TestimonialContentService>();
builder.Services.AddScoped<IHomeContentService, HomeContentService>();
builder.Services.AddScoped<IServiceContentService, ServiceContentService>();
builder.Services.AddScoped<IServicesContentService, ServicesContentService>();
builder.Services.AddScoped<IAboutContentService, AboutContentService>();
builder.Services.AddScoped<IAboutCmsService, AboutCmsService>();
builder.Services.AddScoped<IServicesPageCmsService, ServicesPageCmsService>();
builder.Services.AddScoped<IProjectsPageCmsService, ProjectsPageCmsService>();
builder.Services.AddScoped<ITeamPageCmsService, TeamPageCmsService>();
builder.Services.AddScoped<IProjectsContentService, ProjectsContentService>();

builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));
builder.Services.AddSingleton<MediaFilePaths>();
builder.Services.AddScoped<IMediaStorageService, LocalMediaStorageService>();

var app = builder.Build();
var publicUrls = app.Services.GetRequiredService<PublicSiteUrls>();
await IdentityDatabaseInitializer.InitializeAsync(app.Services, app.Configuration, app.Environment);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var path = context.Request.Path;
        if (!publicUrls.IsIndexable || path.StartsWithSegments("/admin") || path.StartsWithSegments("/dashboard") ||
            path.StartsWithSegments("/Error") || path.StartsWithSegments("/not-found") || context.Response.StatusCode >= 400)
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        return Task.CompletedTask;
    });
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Transitional alias for unchanged public JSON image paths; no legacy script ownership.
// New Razor components use image/, css/, js/ and data/ directly.
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/assets/uploads"),
    branch => branch.UseStaticFiles(new StaticFileOptions { RequestPath = "/assets" }));
app.MapStaticAssets();
app.MapPublicDiscovery();
// Runtime uploads are not build-manifest assets. Only canonical raster paths can be read.
// There is no HTTP upload endpoint; writes run in authenticated interactive Admin forms.
app.MapMethods("/uploads/{folder}/{file}", ["GET", "HEAD"], (string folder, string file, HttpContext context, MediaFilePaths paths) =>
{
    var path = $"uploads/{folder}/{file}";
    if (!NexNovaCo.Web.Models.MediaPolicy.IsGenerated(path) || !paths.Exists(path)) return Results.NotFound();
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    return Results.File(paths.PhysicalPath(path), NexNovaCo.Web.Models.MediaPolicy.ContentType(path));
}).AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// A normal HTTP POST, including from interactive layouts. No cookie mutation on SignalR.
app.MapPost("/admin/logout", async (HttpContext context, IAntiforgery antiforgery, SignInManager<ApplicationUser> signIn) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest("Invalid request."); }
    await signIn.SignOutAsync();
    return Results.LocalRedirect("/admin/login");
}).RequireAuthorization();

app.Run();

public partial class Program;
