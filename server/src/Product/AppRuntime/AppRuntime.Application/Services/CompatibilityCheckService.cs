using AppRuntime.Contracts.DTOs;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Kernel.Results;

namespace AppRuntime.Application.Services;

/// <summary>
/// Stub implementation of compatibility check. Returns compatible by default.
/// Step 3 will add snapshot/metadata and support matrix; this satisfies the BFF contract for Step 2.
/// </summary>
public sealed class CompatibilityCheckService : ICompatibilityCheckService
{
    public Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        CancellationToken cancellationToken = default)
    {
        var dto = new CompatibilityCheckResultDto
        {
            IsCompatible = true,
            MissingComponentTypes = [],
            IncompatibleVersions = [],
            SupportedSchemaVersions = ["1.0"]
        };
        return Task.FromResult(Result<CompatibilityCheckResultDto>.Success(dto));
    }

    public Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(
        Guid applicationReleaseId,
        Guid runtimeVersionId,
        CancellationToken cancellationToken = default)
    {
        var dto = new CompatibilityCheckResultDto
        {
            IsCompatible = true,
            MissingComponentTypes = [],
            IncompatibleVersions = [],
            SupportedSchemaVersions = ["1.0"]
        };
        return Task.FromResult(Result<CompatibilityCheckResultDto>.Success(dto));
    }
}
