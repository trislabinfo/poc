using BuildingBlocks.Web.Modules;
using Feature.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Feature.Module;

/// <summary>
/// Feature module composition root (startup) and migration metadata.
/// </summary>
public sealed class FeatureModule : BaseModule
{
    /// <inheritdoc />
    public override string ModuleName => "Feature";

    /// <inheritdoc />
    public override string SchemaName => "feature";

    /// <inheritdoc />
    protected override void RegisterModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register Domain → Application → Infrastructure when implemented.
        services.AddFeatureApi();
    }
}
