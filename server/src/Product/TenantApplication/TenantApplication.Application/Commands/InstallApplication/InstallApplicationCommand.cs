using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.DTOs;

namespace TenantApplication.Application.Commands.InstallApplication;

public sealed record InstallApplicationCommand(
    Guid TenantId,
    Guid ApplicationReleaseId,
    string Name,
    string Slug,
    string? ConfigurationJson = null)
    : IApplicationRequest<Result<TenantApplicationDto>>, ITransactionalCommand, ITenantApplicationCommand;
