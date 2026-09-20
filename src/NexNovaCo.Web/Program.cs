using NexNovaCo.Web.Components;

using MudBlazor.Services;
using NexNovaCo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddSingleton<IProjectCatalog, ProjectCatalog>();
builder.Services.AddSingleton<IHomeContentService, HomeContentService>();
builder.Services.AddSingleton<IServicesContentService, ServicesContentService>();
builder.Services.AddSingleton<IAboutContentService, AboutContentService>();
builder.Services.AddSingleton<IProjectsContentService, ProjectsContentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

// Transitional alias for unchanged legacy JSON image paths and dormant page scripts.
// New Razor components use image/, css/, js/ and data/ directly.
app.UseStaticFiles(new StaticFileOptions { RequestPath = "/assets" });
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
