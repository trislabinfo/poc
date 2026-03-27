using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Microsoft.Extensions.Logging;
using Tenant.Contracts.Services;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.DatabaseProvisioning;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.CreateEnvironment;

public sealed class CreateEnvironmentCommandHandler
    : IApplicationRequestHandler<CreateEnvironmentCommand, Result<TenantApplicationEnvironmentDto>>
{
    private readonly ITenantApplicationRepository _appRepository;
    private readonly IDatabaseProvisioner _databaseProvisioner;
    private readonly ITenantResolverService _tenantResolver;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateEnvironmentCommandHandler> _logger;

    public CreateEnvironmentCommandHandler(
        ITenantApplicationRepository appRepository,
        IDatabaseProvisioner databaseProvisioner,
        ITenantResolverService tenantResolver,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateEnvironmentCommandHandler> logger)
    {
        _appRepository = appRepository;
        _databaseProvisioner = databaseProvisioner;
        _tenantResolver = tenantResolver;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<TenantApplicationEnvironmentDto>> HandleAsync(
        CreateEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        var app = await _appRepository.GetByIdAsync(request.TenantApplicationId, cancellationToken);
        if (app == null)
        {
            _logger.LogWarning("CreateEnvironment: tenant application not found. TenantApplicationId={TenantApplicationId}. Ensure Step 4 (Install tenant application) was run and the ID matches the response.",
                request.TenantApplicationId);
            return Result<TenantApplicationEnvironmentDto>.Failure(
                Error.NotFound("TenantApplication.NotFound", "Tenant application not found. Run Step 4 (Install tenant application) first and use the tenant application ID from that response."));
        }
        if (app.TenantId != request.TenantId)
        {
            _logger.LogWarning("CreateEnvironment: tenant application {TenantApplicationId} does not belong to tenant {TenantId}.", request.TenantApplicationId, request.TenantId);
            return Result<TenantApplicationEnvironmentDto>.Failure(
                Error.NotFound("TenantApplication.NotFound", "Tenant application not found for this tenant."));
        }

        var result = app.CreateEnvironment(request.Name, request.EnvironmentType, _dateTimeProvider);
        if (result.IsFailure) return Result<TenantApplicationEnvironmentDto>.Failure(result.Error);

        var env = result.Value;

        // Generate database name: {tenant-slug}-{tenantapplicationSlug}-{environmentName}
        // Resolve tenant via contract (in-process or HTTP); fallback slug if Tenant service is unreachable or returns 404/error.
        var tenantResult = await _tenantResolver.GetByIdAsync(app.TenantId, cancellationToken);
        var tenant = tenantResult.IsSuccess ? tenantResult.Value : null;
        var tenantSlug = tenant?.Slug;
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            _logger.LogWarning(
                "Tenant {TenantId} not resolved (Tenant service returned 404 or error: {Failure}); using fallback slug for database name.",
                app.TenantId,
                tenantResult.IsFailure ? tenantResult.Error.Message : "slug empty");
            tenantSlug = "t" + app.TenantId.ToString("N")[..12];
        }
        var normalizedEnvName = request.Name.Trim().ToLowerInvariant().Replace(" ", "-");
        var databaseName = $"{tenantSlug}-{app.Slug}-{normalizedEnvName}";

        // Create database
        string connectionString;
        try
        {
            connectionString = await _databaseProvisioner.CreateDatabaseAsync(databaseName, cancellationToken);
            env.SetDatabaseInfo(databaseName, connectionString, _dateTimeProvider);
            _logger.LogInformation("Created database {DatabaseName} for environment {EnvironmentId}.", databaseName, env.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database {DatabaseName} for environment.", databaseName);
            return Result<TenantApplicationEnvironmentDto>.Failure(
                Error.Failure("TenantApplication.DatabaseCreationFailed", $"Failed to create database: {ex.Message}"));
        }

        _appRepository.Update(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantApplicationEnvironmentDto>.Success(new TenantApplicationEnvironmentDto(
            env.Id,
            env.TenantApplicationId,
            env.Name,
            env.EnvironmentType,
            env.ApplicationReleaseId,
            env.ReleaseVersion,
            env.IsActive,
            env.DeployedAt,
            env.CreatedAt));
    }
}
