using BuildingBlocks.Web.Modules;
using Identity.Api;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Module;

/// <summary>
/// Identity module composition root (startup) and migration metadata.
/// </summary>
public sealed class IdentityModule : BaseModule
{
    /// <inheritdoc />
    public override string ModuleName => "Identity";

    /// <inheritdoc />
    public override string SchemaName => "identity";

    /// <inheritdoc />
    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityApplication();
        services.AddIdentityInfrastructure(configuration, SchemaName);
        services.AddIdentityApi();
    }
}
