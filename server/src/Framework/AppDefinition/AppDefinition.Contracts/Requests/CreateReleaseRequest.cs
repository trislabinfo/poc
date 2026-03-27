namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating an application release (AppBuilder and TenantApplication).</summary>
public sealed record CreateReleaseRequest(
    Guid AppDefinitionId,
    string Version,
    int Major,
    int Minor,
    int Patch,
    string ReleaseNotes,
    string NavigationJson,
    string PageJson,
    string DataSourceJson,
    string EntityJson,
    Guid ReleasedBy);
