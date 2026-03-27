using AppDefinition.Application;
using BuildingBlocks.Web.Modules;
using Capabilities.DatabaseSchema.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantApplication.Api;
using TenantApplication.Application;
using TenantApplication.Infrastructure;

namespace TenantApplication.Module;

/// <summary>TenantApplication module composition root (tenantapplication schema, install/custom apps, definition CRUD).</summary>
public sealed class TenantApplicationModule : BaseModule
{
    /// <inheritdoc />
    public override string ModuleName => "TenantApplication";

    /// <inheritdoc />
    public override string SchemaName => "tenantapplication";

    /// <inheritdoc />
    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppDefinitionApplication();
        services.AddDatabaseSchema(); // Schema derivation, comparison, DDL generation
        services.AddTenantApplication();
        services.AddTenantApplicationInfrastructure(configuration);
        services.AddTenantApplicationApi();
    }
}
