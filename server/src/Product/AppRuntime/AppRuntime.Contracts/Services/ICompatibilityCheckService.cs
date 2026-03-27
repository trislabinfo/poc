using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Contracts.Services;

public interface ICompatibilityCheckService
{
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default);

    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        Guid runtimeVersionId,
        CancellationToken cancellationToken = default);
}
