namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating a navigation definition (AppBuilder and TenantApplication).</summary>
public sealed record CreateNavigationRequest(
    Guid AppDefinitionId,
    string Name,
    string ConfigurationJson = "{}");
