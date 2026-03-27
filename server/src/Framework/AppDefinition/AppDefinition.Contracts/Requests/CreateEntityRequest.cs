namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating an entity definition (AppBuilder and TenantApplication).</summary>
public sealed record CreateEntityRequest(
    Guid AppDefinitionId,
    string Name,
    string DisplayName,
    string? Description = null,
    string AttributesJson = "{}",
    string? PrimaryKey = null);
