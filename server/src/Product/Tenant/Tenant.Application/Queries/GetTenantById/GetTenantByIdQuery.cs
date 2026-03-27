using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Tenant.Application.DTOs;

namespace Tenant.Application.Queries.GetTenantById;

public sealed record GetTenantByIdQuery(Guid TenantId) : IApplicationRequest<Result<TenantDto>>;
