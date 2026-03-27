using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateReleaseDdlScripts;

public sealed record UpdateReleaseDdlScriptsCommand(
    Guid AppDefinitionId,
    Guid ReleaseId,
    string DdlScriptsJson)
    : IApplicationRequest<Result>;
