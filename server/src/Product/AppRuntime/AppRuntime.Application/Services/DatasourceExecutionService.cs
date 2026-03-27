using AppRuntime.Contracts.DTOs;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Kernel.Results;
using System.Text.Json;

namespace AppRuntime.Application.Services;

/// <summary>
/// Executes a datasource from snapshot DataSourceJson. Stub implementation: finds definition by id and returns mock data.
/// Engine registry and real execution can be added later per Step 6 plan.
/// </summary>
public sealed class DatasourceExecutionService : IDatasourceExecutionService
{
    private readonly IReleaseSnapshotProvider _snapshotProvider;

    public DatasourceExecutionService(IReleaseSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public async Task<Result<DatasourceExecuteResultDto>> ExecuteAsync(
        Guid applicationReleaseId,
        string datasourceId,
        CancellationToken cancellationToken = default)
    {
        var snapshotResult = await _snapshotProvider.GetDataSourceJsonAsync(applicationReleaseId, cancellationToken);
        if (snapshotResult.IsFailure)
            return Result<DatasourceExecuteResultDto>.Failure(snapshotResult.Error);

        var dataSourceJson = snapshotResult.Value;
        if (string.IsNullOrWhiteSpace(dataSourceJson))
            return Result<DatasourceExecuteResultDto>.Failure(
                Error.Validation("AppRuntime.Execution.DataSourceJson", "DataSourceJson is required."));
        if (string.IsNullOrWhiteSpace(datasourceId))
            return Result<DatasourceExecuteResultDto>.Failure(
                Error.Validation("AppRuntime.Execution.DatasourceId", "DatasourceId is required."));

        JsonElement root;
        try
        {
            root = JsonDocument.Parse(dataSourceJson).RootElement;
        }
        catch (JsonException ex)
        {
            return Result<DatasourceExecuteResultDto>.Failure(
                Error.Validation("AppRuntime.Execution.InvalidJson", ex.Message));
        }

        // Snapshot may store datasources as array or object; support array of { "id": "...", ... }
        JsonElement? definition = null;
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var idProp) &&
                    idProp.GetString()?.Equals(datasourceId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    definition = item;
                    break;
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("id", out var idProp) &&
                 idProp.GetString()?.Equals(datasourceId, StringComparison.OrdinalIgnoreCase) == true)
        {
            definition = root;
        }

        if (definition == null)
            return Result<DatasourceExecuteResultDto>.Failure(
                Error.NotFound("AppRuntime.Execution.DatasourceNotFound", $"Datasource '{datasourceId}' not found in snapshot."));

        // Stub: return mock data. Replace with engine selection and real execution later.
        var stubData = new { datasourceId, executedAt = DateTime.UtcNow, rows = new[] { new { id = 1, label = "Stub row" } } };
        var dto = new DatasourceExecuteResultDto { Data = stubData, SchemaVersion = "1.0" };
        return Result<DatasourceExecuteResultDto>.Success(dto);
    }
}
