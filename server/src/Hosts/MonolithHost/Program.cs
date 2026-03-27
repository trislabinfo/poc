using AppBuilder.Application;
using AppRuntime.BFF;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Web.Extensions;
using Capabilities.BackgroundJobs.Hangfire;
using Capabilities.ErrorTracking.Sentry;
using Capabilities.Logging.Serilog;
using Capabilities.Messaging;
using Feature.Module;
using Identity.Application.Services;
using Identity.Contracts.Services;
using Identity.Module;
using Tenant.Module;
using TenantApplication.Application;
using TenantApplication.Module;
using User.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Discover application modules (each exposes its Application assembly).
IApplicationModule[] applicationModules =
{
    new Tenant.Application.TenantApplicationModule(),
    new TenantApplicationApplicationModule(),
    new Identity.Application.IdentityApplicationModule(),
    new AppBuilderApplicationModule(),
};

var applicationAssemblies = applicationModules
    .Select(m => m.ApplicationAssembly)
    .ToArray();

// BuildingBlocks: infrastructure only. Request dispatch (handlers, validators, pipeline) via capability.
builder.Services.AddBuildingBlocks(applicationAssemblies);
builder.Services.AddRequestDispatch(applicationAssemblies);

// BuildingBlocks: health checks (database, redis when connection strings present)
builder.AddBuildingBlocksHealthChecks();

// Capability: Serilog structured logging
builder.AddSerilogStructuredLogging();

// Capability: Hangfire background jobs (when Database connection string present)
builder.AddHangfireBackgroundJobs();

// Capability: Sentry error tracking (when Sentry:Dsn configured)
builder.AddSentryErrorTracking();

// Load all modules unconditionally (monolith topology)
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<TenantApplicationModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);
builder.Services.AddModule<FeatureModule>(builder.Configuration);
builder.Services.AddModule<AppBuilder.Module.AppBuilderModule>(builder.Configuration);
builder.Services.AddModule<AppRuntime.Module.AppRuntimeModule>(builder.Configuration);

// Runtime BFF: in-process (resolve, snapshot, initial-view). When Runtime BFF host forwards to Monolith, Monolith must have IRuntimeApi and BFF controllers.
builder.Services.AddRuntimeBff();

// Tenant → Identity: in-process (MediatR) when Monolith
builder.Services.AddScoped<IIdentityApplicationService, IdentityApplicationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: allow dashboard/builder/runtime clients (e.g. http://localhost:5174) to call monolith when credentials: 'include'
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
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
        })
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// BuildingBlocks middleware (order: correlation -> exception -> request logging -> tenant)
app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseRequestLogging();
app.UseTenantResolution();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseModule<TenantModule>();
app.UseModule<TenantApplicationModule>();
app.UseModule<IdentityModule>();
app.UseModule<UserModule>();
app.UseModule<FeatureModule>();
app.UseModule<AppBuilder.Module.AppBuilderModule>();
app.UseModule<AppRuntime.Module.AppRuntimeModule>();

app.UseCors();
// Skip HTTPS redirect in Development so dashboard/builder can POST to http://localhost:8080 without 307
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// BuildingBlocks health endpoints: /health, /health/ready, /health/live
app.MapBuildingBlocksHealthChecks();

// Capability: Hangfire dashboard at /admin/jobs (when Hangfire configured)
app.UseHangfireDashboard();

app.MapDefaultEndpoints();

await app.RunAsync();
