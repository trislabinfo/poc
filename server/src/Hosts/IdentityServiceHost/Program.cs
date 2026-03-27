using BuildingBlocks.Web.Extensions;
using Capabilities.Logging.Serilog;
using Capabilities.Messaging;
using Identity.Module;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var applicationAssemblies = new[] { new Identity.Application.IdentityApplicationModule().ApplicationAssembly };
builder.Services.AddBuildingBlocks(applicationAssemblies);
builder.Services.AddRequestDispatch(applicationAssemblies);
builder.AddBuildingBlocksHealthChecks();
builder.AddSerilogStructuredLogging();

builder.Services.AddModule<IdentityModule>(builder.Configuration);

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

app.UseModule<IdentityModule>();

// Skip HTTPS redirect when behind API Gateway (gateway terminates TLS).
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
