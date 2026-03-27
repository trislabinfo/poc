namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for navigation definition (AppBuilder and TenantApplication).</summary>
public sealed record NavigationDefinitionDto(
    Guid Id,
    Guid AppDefinitionId,
    string Name,
    string ConfigurationJson,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
