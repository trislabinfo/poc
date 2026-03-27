using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Queries.GetMigrationById;

public sealed record GetMigrationByIdQuery(Guid MigrationId) : IApplicationRequest<Result<TenantApplicationMigrationDto>>;
