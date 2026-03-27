using BuildingBlocks.Kernel.Domain;
using MediatR;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// Wraps an <see cref="IDomainEvent"/> for the MediatR pipeline so dispatch stays behind <see cref="IDomainEventDispatcher"/>.
/// </summary>
public sealed record DomainEventEnvelope(IDomainEvent Event) : INotification;
