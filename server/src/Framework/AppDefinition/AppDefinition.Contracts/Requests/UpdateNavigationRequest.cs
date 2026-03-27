namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for updating a navigation definition (AppBuilder and TenantApplication).</summary>
public sealed record UpdateNavigationRequest(
    string Name,
    string ConfigurationJson = "{}");
