using BuildingBlocks.Web.Extensions;
using Capabilities.Logging.Serilog;
using Capabilities.Messaging;
using Tenant.Contracts.Services;
using TenantApplication.Application;
using TenantApplication.Infrastructure.Clients;
using TenantApplication.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var applicationAssemblies = new[] { new TenantApplicationApplicationModule().ApplicationAssembly };
builder.Services.AddBuildingBlocks(applicationAssemblies);
builder.Services.AddRequestDispatch(applicationAssemblies);
builder.AddBuildingBlocksHealthChecks();
builder.AddSerilogStructuredLogging();

builder.Services.AddModule<TenantApplicationModule>(builder.Configuration);

// TenantApplication → Tenant: HTTP client (when deployed as microservices)
builder.Services.AddHttpClient<TenantHttpClient>("tenant", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Services:tenant:https"] ?? config["Services:tenant:http"] ?? config["Services:Tenant:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
builder.Services.AddScoped<ITenantResolverService>(sp => sp.GetRequiredService<TenantHttpClient>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseRequestLogging();
app.UseTenantResolution();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseModule<TenantApplicationModule>();

// Skip HTTPS redirect when behind API Gateway (gateway terminates TLS).
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
