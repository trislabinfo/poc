namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Marker interface for domain events raised by entities/aggregates.
/// Dispatch is implementation-specific (e.g. via the request-dispatch capability).
/// </summary>
public interface IDomainEvent
{
}

