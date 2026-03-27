using AppRuntime.Contracts.Services;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Services;

/// <summary>
/// Default snapshot provider when running without BFF (e.g. standalone AppRuntime host). Always returns NotFound.
/// When BFF is used, it registers BffReleaseSnapshotProvider which overrides this.
/// </summary>
public sealed class StubReleaseSnapshotProvider : IReleaseSnapshotProvider
{
    public Task<Result<string>> GetDataSourceJsonAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<string>.Failure(
            Error.NotFound("AppRuntime.Snapshot.NoProvider", "Release snapshot is not available in this host. Use Runtime BFF for execution.")));
    }
}
