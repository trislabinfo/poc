using AppRuntime.Api;
using AppRuntime.Application.Services;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Web.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.Module;

public sealed class AppRuntimeModule : BaseModule
{
    public override string ModuleName => "AppRuntime";
    public override string SchemaName => "appruntime";

    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICompatibilityCheckService, CompatibilityCheckService>();
        services.AddScoped<IReleaseSnapshotProvider, StubReleaseSnapshotProvider>();
        services.AddScoped<IDatasourceExecutionService, DatasourceExecutionService>();
        services.AddAppRuntimeApi();
    }
}
