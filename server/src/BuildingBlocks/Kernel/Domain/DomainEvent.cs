namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Base type for domain events.
/// </summary>
/// <param name="occurredOn">UTC timestamp when the event occurred.</param>
public abstract record DomainEvent(DateTime occurredOn) : IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; } = occurredOn;
}

