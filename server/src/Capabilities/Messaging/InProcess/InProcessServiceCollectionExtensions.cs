using BuildingBlocks.Application.RequestDispatch;
using Capabilities.Messaging.InProcess.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// In-process request dispatch (MediatR) registration. Registers envelope, bridge, pipeline behaviors, validators, and IRequestDispatcher.
/// </summary>
public static class InProcessServiceCollectionExtensions
{
    public static IServiceCollection AddRequestDispatch(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        if (handlerAssemblies.Length == 0)
            return services;

        var handlerTypes = DiscoverHandlerTypes(handlerAssemblies);
        EnsureNoDuplicateHandlers(handlerTypes);

        foreach (var (requestType, responseType, handlerType) in handlerTypes)
        {
            var handlerInterface = typeof(IApplicationRequestHandler<,>).MakeGenericType(requestType, responseType);
            services.AddTransient(handlerInterface, handlerType);

            var envelopeType = typeof(RequestEnvelope<,>).MakeGenericType(requestType, responseType);
            var mediatRHandlerInterface = typeof(IRequestHandler<,>).MakeGenericType(envelopeType, responseType);
            var bridgeType = typeof(BridgeRequestHandler<,>).MakeGenericType(requestType, responseType);
            services.AddTransient(mediatRHandlerInterface, bridgeType);

            RegisterPipelineBehaviors(services, requestType, responseType);
        }

        services.AddValidatorsFromAssemblies(handlerAssemblies);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRRequestDispatcher).Assembly));

        services.AddTransient<IRequestDispatcher, MediatRRequestDispatcher>();
        services.AddTransient<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        services.Configure<PerformanceBehaviorOptions>(opts => opts.ThresholdMilliseconds = 500);

        return services;
    }

    private static List<(Type RequestType, Type ResponseType, Type HandlerType)> DiscoverHandlerTypes(Assembly[] handlerAssemblies)
    {
        var results = new List<(Type, Type, Type)>();
        var handlerInterface = typeof(IApplicationRequestHandler<,>);

        foreach (var assembly in handlerAssemblies)
        {
            foreach (var type in assembly.DefinedTypes.Where(t => t is { IsClass: true, IsAbstract: false }))
            {
                foreach (var intf in type.GetInterfaces())
                {
                    if (!intf.IsGenericType || intf.GetGenericTypeDefinition() != handlerInterface)
                        continue;

                    var args = intf.GetGenericArguments();
                    if (args.Length != 2)
                        continue;

                    results.Add((args[0], args[1], type.AsType()));
                    break;
                }
            }
        }

        return results;
    }

    private static void EnsureNoDuplicateHandlers(List<(Type RequestType, Type ResponseType, Type HandlerType)> handlerTypes)
    {
        var duplicates = handlerTypes
            .GroupBy(x => (x.RequestType, x.ResponseType))
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
            return;

        var message = string.Join("; ", duplicates.Select(g =>
        {
            var (requestType, responseType) = g.Key;
            var handlerNames = string.Join(", ", g.Select(x => x.HandlerType.FullName));
            return $"Multiple handlers for ({requestType.Name}, {responseType.Name}): {handlerNames}";
        }));
        throw new InvalidOperationException(
            $"Duplicate request handlers are not allowed. Each (Request, Response) pair must have exactly one handler. {message}");
    }

    private static void RegisterPipelineBehaviors(IServiceCollection services, Type requestType, Type responseType)
    {
        var envelopeType = typeof(RequestEnvelope<,>).MakeGenericType(requestType, responseType);
        var mediatRBehaviorInterface = typeof(IPipelineBehavior<,>).MakeGenericType(envelopeType, responseType);
        var ourPipelineInterface = typeof(IRequestPipelineBehavior<,>).MakeGenericType(requestType, responseType);

        var validationType = typeof(ValidationBehavior<,>).MakeGenericType(requestType, responseType);
        var loggingType = typeof(LoggingBehavior<,>).MakeGenericType(requestType, responseType);
        var performanceType = typeof(PerformanceBehavior<,>).MakeGenericType(requestType, responseType);
        var adapterType = typeof(MediatRPipelineBehaviorAdapter<,>).MakeGenericType(requestType, responseType);

        services.AddTransient(ourPipelineInterface, validationType);
        services.AddTransient(ourPipelineInterface, loggingType);
        services.AddTransient(ourPipelineInterface, performanceType);
        services.AddTransient(mediatRBehaviorInterface, adapterType);
    }
}
