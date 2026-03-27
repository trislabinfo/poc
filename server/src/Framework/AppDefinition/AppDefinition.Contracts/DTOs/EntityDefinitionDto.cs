namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for entity definition (AppBuilder and TenantApplication).</summary>
public sealed record EntityDefinitionDto(
    Guid Id,
    Guid AppDefinitionId,
    string Name,
    string DisplayName,
    string? Description,
    string AttributesJson,
    string? PrimaryKey,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
