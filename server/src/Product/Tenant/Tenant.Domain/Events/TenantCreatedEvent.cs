using BuildingBlocks.Kernel.Domain;

namespace Tenant.Domain.Events;

public sealed record TenantCreatedEvent(
    Guid TenantId,
    string Name,
    string Slug,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
