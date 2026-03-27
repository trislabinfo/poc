namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for updating a data source definition (AppBuilder and TenantApplication).</summary>
public sealed record UpdateDataSourceRequest(
    string Name,
    string ConfigurationJson = "{}");
