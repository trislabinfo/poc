using BuildingBlocks.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Extensions;

public static class BuildingBlocksExtensions
{
    /// <summary>
    /// Registers BuildingBlocks infrastructure. For request dispatch, hosts must call
    /// <c>AddRequestDispatch(applicationAssemblies)</c> from Capabilities.Messaging.
    /// </summary>
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        params System.Reflection.Assembly[] _)
    {
        services.AddBuildingBlocksInfrastructure();
        return services;
    }
}
