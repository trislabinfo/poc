using BuildingBlocks.Kernel.Domain;

namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Publishes domain events for processing. Optional; implementation lives in the request-dispatch capability.
/// </summary>
public interface IDomainEventDispatcher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
