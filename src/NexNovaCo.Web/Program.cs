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
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(identityConnection.ConnectionString));
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
builder.Services.AddSingleton<IProjectCatalog, ProjectCatalog>();
builder.Services.AddSingleton<IMemberCatalog, MemberCatalog>();
builder.Services.AddSingleton<ITeamContentService, TeamContentService>();
builder.Services.AddSingleton<IContactContentService, ContactContentService>();
builder.Services.AddSingleton<IContactFormService, DemoContactFormService>();
builder.Services.AddSingleton<IHomeContentService, HomeContentService>();
builder.Services.AddSingleton<IServicesContentService, ServicesContentService>();
builder.Services.AddSingleton<IAboutContentService, AboutContentService>();
builder.Services.AddSingleton<IProjectsContentService, ProjectsContentService>();

var app = builder.Build();
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Transitional alias for unchanged public JSON image paths; no legacy script ownership.
// New Razor components use image/, css/, js/ and data/ directly.
app.UseStaticFiles(new StaticFileOptions { RequestPath = "/assets" });
app.MapStaticAssets();
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
