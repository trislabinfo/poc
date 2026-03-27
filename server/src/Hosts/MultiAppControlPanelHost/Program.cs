using AppBuilder.Module;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Web.Extensions;
using Capabilities.Messaging;
using Feature.Module;
using Identity.Application.Services;
using Identity.Contracts.Services;
using Identity.Module;
using Tenant.Infrastructure.Clients;
using Tenant.Module;
using TenantApplication.Application;
using User.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var loadedModules = builder.Configuration.GetSection("LoadedModules").Get<string[]>() ?? Array.Empty<string>();

// Register MediatR + validators from loaded modules that expose an application assembly
var applicationModules = new List<IApplicationModule>();
if (loadedModules.Contains("TenantManagement")) applicationModules.Add(new Tenant.Application.TenantApplicationModule());
if (loadedModules.Contains("Identity")) applicationModules.Add(new Identity.Application.IdentityApplicationModule());
if (loadedModules.Contains("TenantApplication")) applicationModules.Add(new TenantApplicationApplicationModule());
if (loadedModules.Contains("AppBuilder")) applicationModules.Add(new AppBuilder.Application.AppBuilderApplicationModule());
if (applicationModules.Count > 0)
{
    var applicationAssemblies = applicationModules.Select(m => m.ApplicationAssembly).ToArray();
    builder.Services.AddBuildingBlocks(applicationAssemblies);
    builder.Services.AddRequestDispatch(applicationAssemblies);
}

foreach (var module in loadedModules)
{
    switch (module)
    {
        case "TenantManagement":
            builder.Services.AddModule<TenantModule>(builder.Configuration);
            break;
        case "Identity":
            builder.Services.AddModule<IdentityModule>(builder.Configuration);
            break;
        case "UserManagement":
            builder.Services.AddModule<UserModule>(builder.Configuration);
            break;
        case "FeatureManagement":
            builder.Services.AddModule<FeatureModule>(builder.Configuration);
            break;
        case "TenantApplication":
            builder.Services.AddModule<TenantApplication.Module.TenantApplicationModule>(builder.Configuration);
            break;
        case "AppBuilder":
            builder.Services.AddModule<AppBuilderModule>(builder.Configuration);
            break;
        default:
            throw new InvalidOperationException($"Unknown module: {module}");
    }
}

// Tenant → Identity: in-process when both loaded, HTTP when only Tenant is loaded
var hasIdentity = loadedModules.Contains("Identity");
var hasTenant = loadedModules.Contains("TenantManagement");
if (hasTenant && hasIdentity)
    builder.Services.AddScoped<IIdentityApplicationService, IdentityApplicationService>();
else if (hasTenant && !hasIdentity)
{
    builder.Services.AddHttpClient<IdentityHttpClient>("identity", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["Services:runtime:https"] ?? config["Services:runtime:http"] ?? config["Services:Identity:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();
    builder.Services.AddScoped<IIdentityApplicationService>(sp => sp.GetRequiredService<IdentityHttpClient>());
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure module middleware for the loaded modules (same order as registration).
foreach (var module in loadedModules)
{
    switch (module)
    {
        case "TenantManagement":
            app.UseModule<TenantModule>();
            break;
        case "Identity":
            app.UseModule<IdentityModule>();
            break;
        case "UserManagement":
            app.UseModule<UserModule>();
            break;
        case "FeatureManagement":
            app.UseModule<FeatureModule>();
            break;
        case "TenantApplication":
            app.UseModule<TenantApplication.Module.TenantApplicationModule>();
            break;
        case "AppBuilder":
            app.UseModule<AppBuilderModule>();
            break;
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
