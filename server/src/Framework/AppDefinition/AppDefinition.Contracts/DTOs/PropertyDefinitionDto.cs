using AppDefinition.Domain.Enums;

namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for property definition (AppBuilder and TenantApplication).</summary>
public sealed record PropertyDefinitionDto(
    Guid Id,
    Guid EntityDefinitionId,
    string Name,
    string DisplayName,
    PropertyDataType DataType,
    bool IsRequired,
    string? DefaultValue,
    string ValidationRulesJson,
    int Order,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
