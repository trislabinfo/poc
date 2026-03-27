using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.ApproveRelease;

public sealed record ApproveReleaseCommand(
    Guid AppDefinitionId,
    Guid ReleaseId,
    Guid ApprovedBy)
    : IApplicationRequest<Result>;
