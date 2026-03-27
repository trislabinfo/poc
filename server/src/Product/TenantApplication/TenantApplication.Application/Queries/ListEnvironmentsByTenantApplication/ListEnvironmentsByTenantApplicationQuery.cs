using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Queries.ListEnvironmentsByTenantApplication;

public sealed record ListEnvironmentsByTenantApplicationQuery(Guid TenantApplicationId)
    : IApplicationRequest<Result<IReadOnlyList<TenantApplicationEnvironmentDto>>>;
