namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for updating an entity definition (AppBuilder and TenantApplication).</summary>
public sealed record UpdateEntityRequest(
    string Name,
    string DisplayName,
    string? Description = null,
    string? AttributesJson = null,
    string? PrimaryKey = null);
