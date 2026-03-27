using AppDefinition.Domain.Enums;

namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for relation definition (AppBuilder and TenantApplication).</summary>
public sealed record RelationDefinitionDto(
    Guid Id,
    Guid SourceEntityId,
    Guid TargetEntityId,
    string Name,
    RelationType RelationType,
    bool CascadeDelete,
    DateTime CreatedAt);
