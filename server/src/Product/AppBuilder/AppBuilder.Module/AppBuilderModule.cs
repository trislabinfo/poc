using AppBuilder.Api;
using AppBuilder.Application;
using AppBuilder.Infrastructure;
using AppDefinition.Application;
using BuildingBlocks.Web.Modules;
using Capabilities.DatabaseSchema.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBuilder.Module;

/// <summary>
/// AppBuilder module composition root (startup) and migration metadata.
/// </summary>
public sealed class AppBuilderModule : BaseModule
{
    /// <inheritdoc />
    public override string ModuleName => "AppBuilder";

    /// <inheritdoc />
    public override string SchemaName => "appbuilder";

    /// <inheritdoc />
    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppDefinitionApplication();
        services.AddDatabaseSchema(); // Schema derivation, comparison, DDL generation
        services.AddAppBuilderApplication();
        services.AddAppBuilderInfrastructure(configuration, SchemaName);
        services.AddAppBuilderApi();
    }
}
