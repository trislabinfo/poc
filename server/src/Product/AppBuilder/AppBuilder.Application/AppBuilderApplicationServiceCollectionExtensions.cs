using BuildingBlocks.Application.RequestDispatch;
using Capabilities.Messaging.InProcess;
using Microsoft.Extensions.DependencyInjection;

namespace AppBuilder.Application;

/// <summary>
/// Application-layer DI for the AppBuilder module.
/// Registers per-module transaction behavior for the request-dispatch pipeline; host composes dispatch via AddRequestDispatch(assemblies).
/// </summary>
public static class AppBuilderApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAppBuilderApplication(this IServiceCollection services)
    {
        services.AddAppBuilderTransactionBehaviors();
        return services;
    }

    /// <summary>
    /// Registers <see cref="Behaviors.AppBuilderTransactionBehavior{TRequest,TResponse}"/> for every (request, response) type handled by this assembly.
    /// Call from hosts that use AddRequestDispatch with AppBuilder.Application.
    /// </summary>
    public static IServiceCollection AddAppBuilderTransactionBehaviors(this IServiceCollection services)
    {
        var assembly = typeof(AppBuilderApplicationServiceCollectionExtensions).Assembly;

        foreach (var (requestType, responseType) in RequestHandlerDiscovery.GetRequestResponseTypes(assembly))
        {
            var pipelineInterface = typeof(IRequestPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviorType = typeof(Behaviors.AppBuilderTransactionBehavior<,>).MakeGenericType(requestType, responseType);
            services.AddTransient(pipelineInterface, behaviorType);
        }

        return services;
    }
}
