using BuildingBlocks.Web.Extensions;
using Capabilities.Logging.Serilog;
using Capabilities.Messaging;
using Identity.Contracts.Services;
using Tenant.Infrastructure.Clients;
using Tenant.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var applicationAssemblies = new[] { new Tenant.Application.TenantApplicationModule().ApplicationAssembly };
builder.Services.AddBuildingBlocks(applicationAssemblies);
builder.Services.AddRequestDispatch(applicationAssemblies);
builder.AddBuildingBlocksHealthChecks();
builder.AddSerilogStructuredLogging();

builder.Services.AddModule<TenantModule>(builder.Configuration);

// Tenant → Identity: HTTP client with resilience (base URL from config: Services:identity:https or Services:Identity:BaseUrl)
builder.Services.AddHttpClient<IdentityHttpClient>("identity", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Services:identity:https"] ?? config["Services:identity:http"] ?? config["Services:Identity:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
builder.Services.AddScoped<IIdentityApplicationService>(sp => sp.GetRequiredService<IdentityHttpClient>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseTenantResolution();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseModule<TenantModule>();

// Skip HTTPS redirect when behind API Gateway (gateway terminates TLS; internal calls are HTTP).
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
