using BuildingBlocks.Web.Extensions;
using BuildingBlocks.Web.AdminNavigation;
using AppBuilderClientHost.Components;
using AppBuilder.Web;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Blazor UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Admin navigation contributions (host-owned defaults until feature modules are added).
builder.Services.AddScoped<IAdminNavigationProvider, AppBuilderClientHost.Components.Navigation.AppBuilderStaticAdminNavigationProvider>();

// Feature UI + chat logic wiring (chat UI lives in AppBuilder.Web).
builder.Services.AddAppBuilderWeb(builder.Configuration);

// /health, /health/ready, /health/live
builder.AddBuildingBlocksHealthChecks();

var app = builder.Build();

// Standard middleware order (same idea as other Hosts).
app.UseCorrelationId();
app.UseRequestLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(AppBuilderWebMarker).Assembly);

app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

app.Run();

