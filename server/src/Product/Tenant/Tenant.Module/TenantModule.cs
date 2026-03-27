using BuildingBlocks.Web.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenant.Api;
using Tenant.Application;
using Tenant.Infrastructure;

namespace Tenant.Module;

/// <summary>
/// Tenant module composition root (startup) and migration metadata.
/// </summary>
public sealed class TenantModule : BaseModule
{
    /// <inheritdoc />
    public override string ModuleName => "Tenant";

    /// <inheritdoc />
    public override string SchemaName => "tenant";

    /// <inheritdoc />
    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTenantApplication();
        services.AddTenantInfrastructure(configuration, SchemaName);
        services.AddTenantApi();
    }
}
