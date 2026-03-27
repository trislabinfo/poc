namespace AppRuntime.Domain.Compatibility;

public interface ICompatibilityChecker
{
    Task<bool> SupportsSchemaVersionAsync(string schemaVersion, CancellationToken cancellationToken = default);
}
