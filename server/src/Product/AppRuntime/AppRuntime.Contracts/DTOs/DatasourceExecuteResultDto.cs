namespace AppRuntime.Contracts.DTOs;

/// <summary>
/// Result of a datasource execution. Data is the engine-specific payload (e.g. rows, scalar).
/// </summary>
public sealed record DatasourceExecuteResultDto
{
    public object? Data { get; init; }
    public string? SchemaVersion { get; init; }
}
