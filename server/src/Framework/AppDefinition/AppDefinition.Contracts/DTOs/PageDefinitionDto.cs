namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for page definition (AppBuilder and TenantApplication).</summary>
public sealed record PageDefinitionDto(
    Guid Id,
    Guid AppDefinitionId,
    string Name,
    string Route,
    string ConfigurationJson,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
