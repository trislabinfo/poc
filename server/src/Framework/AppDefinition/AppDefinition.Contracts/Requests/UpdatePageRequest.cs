namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for updating a page definition (AppBuilder and TenantApplication).</summary>
public sealed record UpdatePageRequest(
    string Name,
    string Route,
    string ConfigurationJson = "{}");
