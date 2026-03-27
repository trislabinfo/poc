using AppRuntime.Contracts.Services;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.BFF.Services;

/// <summary>
/// Provides release snapshot DataSourceJson by querying AppBuilder then TenantApplication (same strategy as BFF GetSnapshot).
/// </summary>
public sealed class BffReleaseSnapshotProvider : IReleaseSnapshotProvider
{
    private readonly IRequestDispatcher _requestDispatcher;

    public BffReleaseSnapshotProvider(IRequestDispatcher requestDispatcher)
    {
        _requestDispatcher = requestDispatcher;
    }

    public async Task<Result<string>> GetDataSourceJsonAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default)
    {
        var appBuilderResult = await _requestDispatcher.SendAsync(
            new AppBuilder.Application.Queries.GetReleaseSnapshot.GetReleaseSnapshotQuery(applicationReleaseId),
            cancellationToken);
        if (appBuilderResult.IsSuccess)
            return Result<string>.Success(appBuilderResult.Value!.DataSourceJson);

        var tenantResult = await _requestDispatcher.SendAsync(
            new TenantApplication.Application.Queries.GetReleaseSnapshot.GetReleaseSnapshotQuery(applicationReleaseId),
            cancellationToken);
        if (tenantResult.IsSuccess)
            return Result<string>.Success(tenantResult.Value!.DataSourceJson);

        return Result<string>.Failure(
            BuildingBlocks.Kernel.Results.Error.NotFound("AppRuntime.Snapshot.NotFound", "Release not found."));
    }
}
