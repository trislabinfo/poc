namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Base type for aggregate roots.
/// </summary>
/// <typeparam name="TId">Aggregate identifier type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
}

