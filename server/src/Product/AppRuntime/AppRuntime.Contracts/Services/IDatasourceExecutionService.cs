using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Contracts.Services;

/// <summary>
/// Executes a datasource by id using the release snapshot. Uses IReleaseSnapshotProvider to get DataSourceJson.
/// </summary>
public interface IDatasourceExecutionService
{
    /// <summary>
    /// Execute a datasource for the given application release.
    /// </summary>
    Task<Result<DatasourceExecuteResultDto>> ExecuteAsync(
        Guid applicationReleaseId,
        string datasourceId,
        CancellationToken cancellationToken = default);
}
