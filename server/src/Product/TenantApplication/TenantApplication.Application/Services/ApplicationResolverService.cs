using BuildingBlocks.Kernel.Results;
using Tenant.Contracts.Services;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Enums;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Services;

public sealed class ApplicationResolverService : IApplicationResolverService
{
    private readonly ITenantResolverService _tenantResolver;
    private readonly ITenantApplicationRepository _tenantApplicationRepository;
    private readonly ITenantApplicationEnvironmentRepository _environmentRepository;
    private readonly ITenantEnvironmentConnectionStringProvider _connectionStringProvider;

    public ApplicationResolverService(
        ITenantResolverService tenantResolver,
        ITenantApplicationRepository tenantApplicationRepository,
        ITenantApplicationEnvironmentRepository environmentRepository,
        ITenantEnvironmentConnectionStringProvider connectionStringProvider)
    {
        _tenantResolver = tenantResolver;
        _tenantApplicationRepository = tenantApplicationRepository;
        _environmentRepository = environmentRepository;
        _connectionStringProvider = connectionStringProvider;
    }

    public async Task<Result<ResolvedApplicationDto>> ResolveByUrlAsync(
        string tenantSlug,
        string appSlug,
        string environment,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            return Result<ResolvedApplicationDto>.Failure(Error.Validation("Resolve.TenantSlug", "Tenant slug is required."));
        if (string.IsNullOrWhiteSpace(appSlug))
            return Result<ResolvedApplicationDto>.Failure(Error.Validation("Resolve.AppSlug", "App slug is required."));

        var tenantResult = await _tenantResolver.GetBySlugAsync(tenantSlug.Trim(), cancellationToken);
        if (tenantResult.IsFailure)
            return Result<ResolvedApplicationDto>.Failure(tenantResult.Error);
        var tenant = tenantResult.Value;
        if (tenant == null)
            return Result<ResolvedApplicationDto>.Failure(Error.NotFound("Resolve.TenantNotFound", "Tenant not found."));

        var app = await _tenantApplicationRepository.GetBySlugAsync(tenant.Id, appSlug.Trim(), cancellationToken);
        if (app == null)
            return Result<ResolvedApplicationDto>.Failure(Error.NotFound("Resolve.ApplicationNotFound", "Application not found."));

        var envType = ParseEnvironmentType(environment);
        var env = await _environmentRepository.GetByTenantAppAndEnvironmentAsync(app.Id, envType, cancellationToken);
        if (env == null)
            return Result<ResolvedApplicationDto>.Failure(Error.NotFound("Resolve.EnvironmentNotFound", "Environment not found."));

        if (env.ApplicationReleaseId == null)
            return Result<ResolvedApplicationDto>.Failure(Error.Validation("Resolve.NoDeployment", "No release deployed to this environment."));

        var config = env.ConfigurationJson ?? "{}";
        var connectionString = _connectionStringProvider.GetConnectionString(
            env.ConnectionString,
            env.DatabaseName,
            tenant.Slug,
            app.Slug,
            env.Name);

        var dto = new ResolvedApplicationDto(
            TenantId: tenant!.Id,
            TenantSlug: tenant.Slug,
            TenantApplicationId: app.Id,
            AppSlug: app.Slug,
            ApplicationReleaseId: env.ApplicationReleaseId.Value,
            EnvironmentConfiguration: config,
            IsTenantRelease: app.IsCustom,
            ConnectionString: connectionString);

        return Result<ResolvedApplicationDto>.Success(dto);
    }

    private static EnvironmentType ParseEnvironmentType(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            return EnvironmentType.Production;
        var normalized = environment.Trim().ToLowerInvariant();
        return normalized switch
        {
            "development" => EnvironmentType.Development,
            "staging" => EnvironmentType.Staging,
            "production" => EnvironmentType.Production,
            _ => EnvironmentType.Production
        };
    }
}
