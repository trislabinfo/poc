namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for updating a property definition (AppBuilder and TenantApplication).</summary>
public sealed record UpdatePropertyRequest(
    string DisplayName,
    bool IsRequired,
    int Order,
    string? DefaultValue = null,
    string? ValidationRulesJson = null);
