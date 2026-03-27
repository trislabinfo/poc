using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Queries.ListMigrationsByEnvironment;

public sealed record ListMigrationsByEnvironmentQuery(Guid EnvironmentId)
    : IApplicationRequest<Result<IReadOnlyList<TenantApplicationMigrationDto>>>;
