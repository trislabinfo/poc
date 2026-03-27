using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Queries.GetTenantApplicationById;

public sealed record GetTenantApplicationByIdQuery(Guid TenantApplicationId) : IApplicationRequest<Result<TenantApplicationDto>>;
