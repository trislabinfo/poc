namespace AppRuntime.Contracts.DTOs;

/// <summary>
/// Request to execute a datasource at runtime. BFF receives this from the client.
/// </summary>
public sealed record DatasourceExecuteRequestDto(
    Guid ApplicationReleaseId,
    string DatasourceId);
