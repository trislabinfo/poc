using BuildingBlocks.Application.RequestDispatch;
using Capabilities.Messaging.InProcess;
using Microsoft.Extensions.DependencyInjection;
using TenantApplication.Application.MigrationExecution;
using TenantApplication.Application.Services;

namespace TenantApplication.Application;

public static class TenantApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTenantApplication(this IServiceCollection services)
    {
        services.AddTenantApplicationTransactionBehaviors();
        services.AddScoped<IMigrationExecutor, MigrationExecutor>();
        services.AddScoped<IApplicationResolverService, ApplicationResolverService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="Behaviors.TenantApplicationTransactionBehavior{TRequest,TResponse}"/> for every (request, response) type handled by this assembly.
    /// Call from hosts that use <see cref="Capabilities.Messaging.MessagingServiceCollectionExtensions.AddRequestDispatch"/> with TenantApplication.Application.
    /// </summary>
    public static IServiceCollection AddTenantApplicationTransactionBehaviors(this IServiceCollection services)
    {
        var assembly = typeof(TenantApplicationServiceCollectionExtensions).Assembly;

        foreach (var (requestType, responseType) in RequestHandlerDiscovery.GetRequestResponseTypes(assembly))
        {
            var pipelineInterface = typeof(IRequestPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviorType = typeof(Behaviors.TenantApplicationTransactionBehavior<,>).MakeGenericType(requestType, responseType);
            services.AddTransient(pipelineInterface, behaviorType);
        }

        return services;
    }
}
