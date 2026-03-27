using BuildingBlocks.Web.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using User.Api;

namespace User.Module;

/// <summary>
/// User module composition root (startup) and migration metadata.
/// </summary>
public sealed class UserModule : BaseModule
{
    /// <inheritdoc />
    public override string ModuleName => "User";

    /// <inheritdoc />
    public override string SchemaName => "user";

    /// <inheritdoc />
    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Domain → Application → Infrastructure when implemented.
        services.AddUserApi();
    }
}
