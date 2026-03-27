using AppDefinition.Contracts.DTOs;
using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace AppRuntime.BFF.Services;

/// <summary>
/// Backend used by the Runtime BFF to obtain data for the runtime client.
/// BFF aggregates and optimizes communication; this abstraction allows in-process (Monolith/DistributedApp)
/// or remote calls (Monolith HTTP, or in future microservices: Tenant Application, Identity).
/// </summary>
public interface IRuntimeApi
{
    Task<Result<ResolvedApplicationDto>> ResolveAsync(string tenantSlug, string appSlug, string environment, CancellationToken cancellationToken = default);
    Task<Result<ApplicationSnapshotDto?>> GetSnapshotAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);
    Task<Result<string?>> GetInitialViewHtmlAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default);
    Task<Result<string?>> GetEntityViewHtmlAsync(Guid applicationReleaseId, Guid entityId, string viewType, CancellationToken cancellationToken = default);
    Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(Guid applicationReleaseId, Guid? runtimeVersionId, CancellationToken cancellationToken = default);
    Task<Result<DatasourceExecuteResultDto>> ExecuteDatasourceAsync(Guid applicationReleaseId, string datasourceId, CancellationToken cancellationToken = default);
}
