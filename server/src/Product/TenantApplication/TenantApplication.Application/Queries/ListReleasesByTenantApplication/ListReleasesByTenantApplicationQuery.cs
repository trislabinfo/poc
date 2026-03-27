using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Queries.ListReleasesByTenantApplication;

public sealed record ListReleasesByTenantApplicationQuery(Guid TenantApplicationId)
    : IApplicationRequest<Result<IReadOnlyList<ApplicationReleaseDto>>>;
