using BuildingBlocks.Application.RequestDispatch;
using System.Reflection;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// Discovers request/response type pairs from assemblies that contain <see cref="IApplicationRequestHandler{TRequest,TResponse}"/> implementations.
/// Used by modules to register their pipeline behaviors for the request-dispatch pipeline.
/// </summary>
public static class RequestHandlerDiscovery
{
    /// <summary>
    /// Returns all (RequestType, ResponseType) pairs for which the given assembly contains an application request handler.
    /// </summary>
    public static IEnumerable<(Type RequestType, Type ResponseType)> GetRequestResponseTypes(Assembly assembly)
    {
        var handlerInterface = typeof(IApplicationRequestHandler<,>);

        foreach (var type in assembly.DefinedTypes.Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var intf in type.GetInterfaces())
            {
                if (!intf.IsGenericType || intf.GetGenericTypeDefinition() != handlerInterface)
                    continue;

                var args = intf.GetGenericArguments();
                if (args.Length != 2)
                    continue;

                yield return (args[0], args[1]);
                break;
            }
        }
    }
}
