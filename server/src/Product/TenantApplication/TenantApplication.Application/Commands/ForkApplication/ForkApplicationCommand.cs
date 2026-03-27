using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Commands.ForkApplication;

public sealed record ForkApplicationCommand(
    Guid TenantId,
    Guid SourceApplicationReleaseId,
    string Name,
    string Slug)
    : IApplicationRequest<Result<TenantApplicationDto>>, ITransactionalCommand, ITenantApplicationCommand;
