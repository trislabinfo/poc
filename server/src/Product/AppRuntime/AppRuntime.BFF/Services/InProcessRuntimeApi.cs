using AppDefinition.Contracts.DTOs;
using AppRuntime.Contracts.DTOs;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Application.Queries.GetReleaseSnapshot;
using TenantApplication.Application.Services;

namespace AppRuntime.BFF.Services;

/// <summary>
/// Runtime API implementation using in-process modules (TenantApplication, AppBuilder, AppRuntime).
/// Used when BFF runs with all modules in the same process (e.g. Distributed App or standalone).
/// </summary>
public sealed class InProcessRuntimeApi : IRuntimeApi
{
    private readonly IApplicationResolverService _resolver;
    private readonly IRequestDispatcher _requestDispatcher;
    private readonly ICompatibilityCheckService _compatibilityCheckService;
    private readonly IDatasourceExecutionService _datasourceExecutionService;

    public InProcessRuntimeApi(
        IApplicationResolverService resolver,
        IRequestDispatcher requestDispatcher,
        ICompatibilityCheckService compatibilityCheckService,
        IDatasourceExecutionService datasourceExecutionService)
    {
        _resolver = resolver;
        _requestDispatcher = requestDispatcher;
        _compatibilityCheckService = compatibilityCheckService;
        _datasourceExecutionService = datasourceExecutionService;
    }

    public Task<Result<ResolvedApplicationDto>> ResolveAsync(string tenantSlug, string appSlug, string environment, CancellationToken cancellationToken = default)
        => _resolver.ResolveByUrlAsync(tenantSlug, appSlug, environment, cancellationToken);

    public async Task<Result<ApplicationSnapshotDto?>> GetSnapshotAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default)
    {
        var appBuilderResult = await _requestDispatcher.SendAsync(
            new AppBuilder.Application.Queries.GetReleaseSnapshot.GetReleaseSnapshotQuery(applicationReleaseId),
            cancellationToken);
        if (appBuilderResult.IsSuccess)
            return Result<ApplicationSnapshotDto?>.Success(appBuilderResult.Value);

        var tenantResult = await _requestDispatcher.SendAsync(
            new GetReleaseSnapshotQuery(applicationReleaseId),
            cancellationToken);
        if (tenantResult.IsSuccess)
            return Result<ApplicationSnapshotDto?>.Success(tenantResult.Value);

        return Result<ApplicationSnapshotDto?>.Failure(BuildingBlocks.Kernel.Results.Error.NotFound("Runtime.Snapshot", "Release not found."));
    }

    public async Task<Result<string?>> GetInitialViewHtmlAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default)
    {
        var appBuilderResult = await _requestDispatcher.SendAsync(
            new AppBuilder.Application.Queries.GetReleaseInitialViewHtml.GetReleaseInitialViewHtmlQuery(applicationReleaseId),
            cancellationToken);
        if (appBuilderResult.IsSuccess && !string.IsNullOrEmpty(appBuilderResult.Value))
            return Result<string?>.Success(appBuilderResult.Value);

        var tenantResult = await _requestDispatcher.SendAsync(
            new TenantApplication.Application.Queries.GetReleaseInitialViewHtml.GetReleaseInitialViewHtmlQuery(applicationReleaseId),
            cancellationToken);
        if (tenantResult.IsSuccess && !string.IsNullOrEmpty(tenantResult.Value))
            return Result<string?>.Success(tenantResult.Value);

        return Result<string?>.Failure(BuildingBlocks.Kernel.Results.Error.NotFound("Runtime.InitialView", "Initial view HTML not found."));
    }

    public async Task<Result<string?>> GetEntityViewHtmlAsync(Guid applicationReleaseId, Guid entityId, string viewType, CancellationToken cancellationToken = default)
    {
        var appBuilderResult = await _requestDispatcher.SendAsync(
            new AppBuilder.Application.Queries.GetReleaseEntityViewHtml.GetReleaseEntityViewHtmlQuery(applicationReleaseId, entityId, viewType),
            cancellationToken);
        if (appBuilderResult.IsSuccess && !string.IsNullOrEmpty(appBuilderResult.Value))
            return Result<string?>.Success(appBuilderResult.Value);

        var tenantResult = await _requestDispatcher.SendAsync(
            new TenantApplication.Application.Queries.GetReleaseEntityViewHtml.GetReleaseEntityViewHtmlQuery(applicationReleaseId, entityId, viewType),
            cancellationToken);
        if (tenantResult.IsSuccess && !string.IsNullOrEmpty(tenantResult.Value))
            return Result<string?>.Success(tenantResult.Value);

        return Result<string?>.Failure(BuildingBlocks.Kernel.Results.Error.NotFound("Runtime.EntityView", "Entity view HTML not found."));
    }

    public Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(Guid applicationReleaseId, Guid? runtimeVersionId, CancellationToken cancellationToken = default)
        => runtimeVersionId.HasValue
            ? _compatibilityCheckService.CheckCompatibilityAsync(applicationReleaseId, runtimeVersionId.Value, cancellationToken)
            : _compatibilityCheckService.CheckCompatibilityAsync(applicationReleaseId, cancellationToken);

    public Task<Result<DatasourceExecuteResultDto>> ExecuteDatasourceAsync(Guid applicationReleaseId, string datasourceId, CancellationToken cancellationToken = default)
        => _datasourceExecutionService.ExecuteAsync(applicationReleaseId, datasourceId, cancellationToken);
}
