using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Contracts.Services;

/// <summary>
/// Provides release snapshot data for runtime execution. Implemented by the BFF (or host) using AppBuilder/TenantApplication.
/// </summary>
public interface IReleaseSnapshotProvider
{
    /// <summary>
    /// Gets the DataSourceJson for the given application release (from platform or tenant snapshot).
    /// </summary>
    Task<Result<string>> GetDataSourceJsonAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default);
}
