using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using MediatR;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// Implements IDomainEventDispatcher using MediatR. Domain events are wrapped in DomainEventEnvelope.
/// </summary>
public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public MediatRDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => _mediator.Publish(new DomainEventEnvelope(domainEvent), cancellationToken);
}
