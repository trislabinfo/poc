using BuildingBlocks.Kernel.Domain;

namespace AppDefinition.Domain.Events;

public sealed record AppDefinitionCreatedEvent(
    Guid AppDefinitionId,
    string Name,
    string Slug,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
