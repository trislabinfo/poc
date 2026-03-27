using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Commands.CreateCustomApplication;

public sealed record CreateCustomApplicationCommand(
    Guid TenantId,
    string Name,
    string Slug,
    string? Description)
    : IApplicationRequest<Result<TenantApplicationDto>>, ITransactionalCommand, ITenantApplicationCommand;
