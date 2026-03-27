using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.ListMigrationsByEnvironment;

public sealed class ListMigrationsByEnvironmentQueryHandler
    : IApplicationRequestHandler<ListMigrationsByEnvironmentQuery, Result<IReadOnlyList<TenantApplicationMigrationDto>>>
{
    private readonly ITenantApplicationMigrationRepository _repository;

    public ListMigrationsByEnvironmentQueryHandler(ITenantApplicationMigrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TenantApplicationMigrationDto>>> HandleAsync(
        ListMigrationsByEnvironmentQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByEnvironmentAsync(request.EnvironmentId, cancellationToken);
        var dtos = list.Select(m => new TenantApplicationMigrationDto(
            m.Id,
            m.TenantApplicationEnvironmentId,
            m.FromReleaseId,
            m.ToReleaseId,
            m.MigrationScriptJson,
            m.Status,
            m.ExecutedAt,
            m.ErrorMessage,
            m.CreatedAt,
            m.UpdatedAt)).ToList();
        return Result<IReadOnlyList<TenantApplicationMigrationDto>>.Success(dtos);
    }
}
