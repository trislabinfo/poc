using AppDefinition.Domain.Enums;

namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating a property definition (AppBuilder and TenantApplication).</summary>
public sealed record CreatePropertyRequest(
    Guid EntityDefinitionId,
    string Name,
    string DisplayName,
    PropertyDataType DataType,
    bool IsRequired,
    int Order,
    string? DefaultValue = null,
    string ValidationRulesJson = "{}");
