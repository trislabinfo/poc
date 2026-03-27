using Capabilities.Messaging.InProcess;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Capabilities.Messaging;

/// <summary>
/// Messaging capability registration. For in-process dispatch use <see cref="InProcessServiceCollectionExtensions.AddRequestDispatch"/>.
/// Out-of-process (e.g. MassTransit) will be added under OutOfProcess folder later.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers in-process request dispatch (MediatR bridge, IRequestDispatcher). Call from hosts that need request/response dispatch.
    /// </summary>
    public static IServiceCollection AddRequestDispatch(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
        => InProcessServiceCollectionExtensions.AddRequestDispatch(services, handlerAssemblies);
}
