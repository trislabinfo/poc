using BuildingBlocks.Application.RequestDispatch;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Repositories;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Application.Queries.GetTenantApplications;

public sealed class GetTenantApplicationsQueryHandler
    : IApplicationRequestHandler<GetTenantApplicationsQuery, IReadOnlyList<TenantApplicationDto>>
{
    private readonly ITenantApplicationRepository _repository;

    public GetTenantApplicationsQueryHandler(ITenantApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TenantApplicationDto>> HandleAsync(GetTenantApplicationsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return list.Select(Map).ToList();
    }

    private static TenantApplicationDto Map(TenantApplicationEntity a) => new(
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
