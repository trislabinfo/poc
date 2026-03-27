using AppRuntime.Module;
using BuildingBlocks.Web.Extensions;
using Capabilities.Logging.Serilog;
using Capabilities.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var applicationAssemblies = new[] { new AppRuntime.Application.AppRuntimeApplicationModule().ApplicationAssembly };
builder.Services.AddBuildingBlocks(applicationAssemblies);
builder.Services.AddRequestDispatch(applicationAssemblies);
builder.AddBuildingBlocksHealthChecks();
builder.AddSerilogStructuredLogging();

builder.Services.AddModule<AppRuntimeModule>(builder.Configuration);

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

app.UseModule<AppRuntimeModule>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapBuildingBlocksHealthChecks();
app.MapDefaultEndpoints();

await app.RunAsync();
