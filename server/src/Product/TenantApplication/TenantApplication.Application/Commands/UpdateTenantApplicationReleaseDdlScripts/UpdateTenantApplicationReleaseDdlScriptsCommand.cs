using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.UpdateTenantApplicationReleaseDdlScripts;

public sealed record UpdateTenantApplicationReleaseDdlScriptsCommand(
    Guid TenantApplicationId,
    Guid ReleaseId,
    string DdlScriptsJson)
    : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
