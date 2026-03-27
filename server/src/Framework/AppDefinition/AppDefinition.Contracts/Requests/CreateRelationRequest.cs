using AppDefinition.Domain.Enums;

namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating a relation definition (AppBuilder and TenantApplication).</summary>
public sealed record CreateRelationRequest(
    Guid SourceEntityId,
    Guid TargetEntityId,
    string Name,
    RelationType RelationType,
    bool CascadeDelete = false);
