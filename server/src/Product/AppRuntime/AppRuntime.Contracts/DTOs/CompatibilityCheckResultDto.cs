namespace AppRuntime.Contracts.DTOs;

public record CompatibilityCheckResultDto
{
    public bool IsCompatible { get; init; }
    public List<string> MissingComponentTypes { get; init; } = [];
    public List<string> IncompatibleVersions { get; init; } = [];
    public string? ErrorMessage { get; init; }
    /// <summary>Schema versions the runtime (and client) support for adapter selection. Per Compatibility and Versioning Framework.</summary>
    public List<string> SupportedSchemaVersions { get; init; } = [];
}
