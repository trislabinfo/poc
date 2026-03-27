using BuildingBlocks.Application.RequestDispatch;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Queries.GetTenantApplications;

public sealed record GetTenantApplicationsQuery(Guid TenantId)
    : IApplicationRequest<IReadOnlyList<TenantApplicationDto>>;
