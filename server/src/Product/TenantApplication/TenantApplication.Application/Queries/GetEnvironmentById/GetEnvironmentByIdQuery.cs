using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Queries.GetEnvironmentById;

public sealed record GetEnvironmentByIdQuery(Guid EnvironmentId) : IApplicationRequest<Result<TenantApplicationEnvironmentDto>>;
