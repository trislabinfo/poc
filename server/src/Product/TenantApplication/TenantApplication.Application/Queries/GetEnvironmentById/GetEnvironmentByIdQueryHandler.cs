using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.GetEnvironmentById;

public sealed class GetEnvironmentByIdQueryHandler
    : IApplicationRequestHandler<GetEnvironmentByIdQuery, Result<TenantApplicationEnvironmentDto>>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;

    public GetEnvironmentByIdQueryHandler(ITenantApplicationEnvironmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantApplicationEnvironmentDto>> HandleAsync(
        GetEnvironmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var env = await _repository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (env == null)
            return Result<TenantApplicationEnvironmentDto>.Failure(
                Error.NotFound("TenantApplication.EnvironmentNotFound", "Environment not found."));
        return Result<TenantApplicationEnvironmentDto>.Success(Map(env));
    }

    private static TenantApplicationEnvironmentDto Map(TenantApplication.Domain.Entities.TenantApplicationEnvironment e) => new(
        e.Id,
        e.TenantApplicationId,
        e.Name,
        e.EnvironmentType,
        e.ApplicationReleaseId,
        e.ReleaseVersion,
        e.IsActive,
        e.DeployedAt,
        e.CreatedAt);
}
