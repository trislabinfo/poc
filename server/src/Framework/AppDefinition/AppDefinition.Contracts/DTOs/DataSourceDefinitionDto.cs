using AppDefinition.Domain.Enums;

namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for data source definition (AppBuilder and TenantApplication).</summary>
public sealed record DataSourceDefinitionDto(
    Guid Id,
    Guid AppDefinitionId,
    string Name,
    DataSourceType Type,
    string ConfigurationJson,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
