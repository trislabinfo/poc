using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Entities;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.GetMigrationById;

public sealed class GetMigrationByIdQueryHandler
    : IApplicationRequestHandler<GetMigrationByIdQuery, Result<TenantApplicationMigrationDto>>
{
    private readonly ITenantApplicationMigrationRepository _repository;

    public GetMigrationByIdQueryHandler(ITenantApplicationMigrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantApplicationMigrationDto>> HandleAsync(
        GetMigrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var migration = await _repository.GetByIdAsync(request.MigrationId, cancellationToken);
        if (migration == null)
            return Result<TenantApplicationMigrationDto>.Failure(
                Error.NotFound("TenantApplication.MigrationNotFound", "Migration not found."));
        return Result<TenantApplicationMigrationDto>.Success(Map(migration));
    }

    private static TenantApplicationMigrationDto Map(TenantApplicationMigration m) => new(
        m.Id,
        m.TenantApplicationEnvironmentId,
        m.FromReleaseId,
        m.ToReleaseId,
        m.MigrationScriptJson,
        m.Status,
        m.ExecutedAt,
        m.ErrorMessage,
        m.CreatedAt,
        m.UpdatedAt);
}
