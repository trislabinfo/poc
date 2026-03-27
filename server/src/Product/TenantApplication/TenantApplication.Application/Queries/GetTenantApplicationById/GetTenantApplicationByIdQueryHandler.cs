using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Repositories;
using Entity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Application.Queries.GetTenantApplicationById;

public sealed class GetTenantApplicationByIdQueryHandler
    : IApplicationRequestHandler<GetTenantApplicationByIdQuery, Result<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;

    public GetTenantApplicationByIdQueryHandler(ITenantApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantApplicationDto>> HandleAsync(
        GetTenantApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.TenantApplicationId, cancellationToken);
        if (app == null)
            return Result<TenantApplicationDto>.Failure(
                Error.NotFound("TenantApplication.NotFound", "Tenant application not found."));
        return Result<TenantApplicationDto>.Success(Map(app));
    }

    private static TenantApplicationDto Map(Entity a) => new(
        a.Id,
        a.TenantId,
        a.ApplicationReleaseId,
        a.ApplicationId,
        a.Major,
        a.Minor,
        a.Patch,
        a.Name,
        a.Slug,
        a.Description,
        a.IsCustom,
        a.SourceApplicationReleaseId,
        a.Status,
        a.ConfigurationJson,
        a.InstalledAt,
        a.ActivatedAt,
        a.CreatedAt);
}
