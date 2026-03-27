using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;
using TenantApplication.Domain.Enums;

namespace TenantApplication.Application.Commands.CreateEnvironment;

public sealed record CreateEnvironmentCommand(
    Guid TenantId,
    Guid TenantApplicationId,
    string Name,
    EnvironmentType EnvironmentType)
    : IApplicationRequest<Result<TenantApplicationEnvironmentDto>>, ITransactionalCommand, ITenantApplicationCommand;
