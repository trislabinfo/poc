using BuildingBlocks.Application.RequestDispatch;
using Capabilities.Messaging.InProcess;
using Microsoft.Extensions.DependencyInjection;
using Tenant.Application.Services;
using Tenant.Contracts.Services;

namespace Tenant.Application;

/// <summary>
/// Application-layer DI for the Tenant module.
/// Registers per-module transaction behavior for the request-dispatch pipeline; host composes dispatch via AddRequestDispatch(assemblies).
/// Registers in-process ITenantResolverService; microservice hosts register HTTP client instead.
/// </summary>
public static class TenantApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTenantApplication(this IServiceCollection services)
    {
        services.AddTenantTransactionBehaviors();
        services.AddScoped<ITenantResolverService, TenantResolverService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="Behaviors.TenantTransactionBehavior{TRequest,TResponse}"/> for every (request, response) type handled by this assembly.
    /// Call from hosts that use <see cref="Capabilities.Messaging.MessagingServiceCollectionExtensions.AddRequestDispatch"/> with Tenant.Application.
    /// </summary>
    public static IServiceCollection AddTenantTransactionBehaviors(this IServiceCollection services)
    {
        var assembly = typeof(TenantApplicationServiceCollectionExtensions).Assembly;

        foreach (var (requestType, responseType) in RequestHandlerDiscovery.GetRequestResponseTypes(assembly))
        {
            var pipelineInterface = typeof(IRequestPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviorType = typeof(Behaviors.TenantTransactionBehavior<,>).MakeGenericType(requestType, responseType);
            services.AddTransient(pipelineInterface, behaviorType);
        }

        return services;
    }
}
