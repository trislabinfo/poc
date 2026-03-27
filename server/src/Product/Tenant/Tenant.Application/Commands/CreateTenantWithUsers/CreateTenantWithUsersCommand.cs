using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Tenant.Application.DTOs;

namespace Tenant.Application.Commands.CreateTenantWithUsers;

public sealed record CreateTenantWithUsersCommand(
    string Name,
    string Slug,
    IReadOnlyList<UserData> Users)
    : IApplicationRequest<Result<TenantWithUsersDto>>, ITransactionalCommand, ITenantCommand;
