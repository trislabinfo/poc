using AppBuilder.Application;
using AppBuilder.Module;
using AppRuntime.BFF;
using AppRuntime.BFF.Services;
using AppRuntime.Module;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Web.Extensions;
using Capabilities.Logging.Serilog;
using Capabilities.Messaging;
using Identity.Contracts.Services;
using Tenant.Infrastructure.Clients;
using Tenant.Module;
using TenantApplication.Application;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Runtime BFF optimizes communication with the runtime client and aggregates data.
// When Monolith URL is set (Monolith topology), BFF calls Monolith over HTTP for backend data.
// Otherwise BFF hosts modules in-process (Distributed App or standalone). In future: auth tokens; microservices (Tenant Application, Identity).
var monolithBase = builder.Configuration["Services:monolith:http"]
    ?? builder.Configuration["Services:Monolith:Http"]
    ?? builder.Configuration["Services__monolith__http"]
    ?? string.Empty;
var useMonolithBackend = !string.IsNullOrWhiteSpace(monolithBase);

if (useMonolithBackend)
{
    builder.Services.AddHttpClient("monolith", client =>
    {
        client.BaseAddress = new Uri(monolithBase.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddStandardResilienceHandler();
    builder.Services.AddScoped<IRuntimeApi, MonolithHttpRuntimeApi>();
    builder.Services.AddRuntimeBffControllers();
}
else
{
    var applicationModules = new IApplicationModule[]
    {
        new Tenant.Application.TenantApplicationModule(),
        new TenantApplicationApplicationModule(),
        new AppBuilderApplicationModule(),
    };
    var applicationAssemblies = applicationModules.Select(m => m.ApplicationAssembly).ToArray();

    builder.Services.AddBuildingBlocks(applicationAssemblies);
    builder.Services.AddRequestDispatch(applicationAssemblies);
    builder.AddBuildingBlocksHealthChecks();
    builder.AddSerilogStructuredLogging();

    builder.Services.AddHttpClient<IdentityHttpClient>("identity", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["Services:identity:https"] ?? config["Services:identity:http"] ?? config["Services:Identity:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddStandardResilienceHandler();
    builder.Services.AddScoped<IIdentityApplicationService>(sp => sp.GetRequiredService<IdentityHttpClient>());

    builder.Services.AddModule<TenantModule>(builder.Configuration);
    builder.Services.AddModule<TenantApplication.Module.TenantApplicationModule>(builder.Configuration);
    builder.Services.AddModule<AppBuilderModule>(builder.Configuration);
    builder.Services.AddModule<AppRuntimeModule>(builder.Configuration);
    builder.Services.AddRuntimeBff();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var configured = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (configured?.Length > 0)
        {
            policy.WithOrigins(configured).AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                try
                {
                    var uri = new Uri(origin);
                    return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                        || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }).AllowAnyMethod().AllowAnyHeader();
        }
    });
});

var app = builder.Build();

app.UseCors();
app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseRequestLogging();

if (!useMonolithBackend)
{
    app.UseTenantResolution();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseModule<TenantModule>();
    app.UseModule<TenantApplication.Module.TenantApplicationModule>();
    app.UseModule<AppBuilderModule>();
    app.UseModule<AppRuntimeModule>();
}

app.UseAuthorization();
app.MapControllers();
if (!useMonolithBackend)
    app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
