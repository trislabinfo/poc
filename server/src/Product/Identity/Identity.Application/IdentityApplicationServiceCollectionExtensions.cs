using BuildingBlocks.Application.RequestDispatch;
using Capabilities.Messaging.InProcess;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

/// <summary>
/// Application-layer DI for the Identity module.
/// Registers per-module transaction behavior for the request-dispatch pipeline; host composes dispatch via AddRequestDispatch(assemblies).
/// </summary>
public static class IdentityApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddIdentityTransactionBehaviors();
        return services;
    }

    /// <summary>
    /// Registers <see cref="Behaviors.IdentityTransactionBehavior{TRequest,TResponse}"/> for every (request, response) type handled by this assembly.
    /// Call from hosts that use AddRequestDispatch with Identity.Application.
    /// </summary>
    public static IServiceCollection AddIdentityTransactionBehaviors(this IServiceCollection services)
    {
        var assembly = typeof(IdentityApplicationServiceCollectionExtensions).Assembly;

        foreach (var (requestType, responseType) in RequestHandlerDiscovery.GetRequestResponseTypes(assembly))
        {
            var pipelineInterface = typeof(IRequestPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviorType = typeof(Behaviors.IdentityTransactionBehavior<,>).MakeGenericType(requestType, responseType);
            services.AddTransient(pipelineInterface, behaviorType);
        }

        return services;
    }
}
