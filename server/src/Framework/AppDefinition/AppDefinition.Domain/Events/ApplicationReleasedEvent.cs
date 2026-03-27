using BuildingBlocks.Kernel.Domain;

namespace AppDefinition.Domain.Events;

public sealed record ApplicationReleasedEvent(
    Guid AppDefinitionId,
    Guid ApplicationReleaseId,
    string Version,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
