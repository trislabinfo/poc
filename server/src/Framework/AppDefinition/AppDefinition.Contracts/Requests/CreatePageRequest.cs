namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating a page definition (AppBuilder and TenantApplication).</summary>
public sealed record CreatePageRequest(
    Guid AppDefinitionId,
    string Name,
    string Route,
    string ConfigurationJson = "{}");
