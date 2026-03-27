using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.UpdateTenantApplicationReleaseDdlScripts;

public sealed class UpdateTenantApplicationReleaseDdlScriptsCommandHandler
    : IApplicationRequestHandler<UpdateTenantApplicationReleaseDdlScriptsCommand, Result>
{
    private readonly ITenantApplicationReleaseRepository _releaseRepository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;

    public UpdateTenantApplicationReleaseDdlScriptsCommandHandler(
        ITenantApplicationReleaseRepository releaseRepository,
        ITenantApplicationUnitOfWork unitOfWork)
    {
        _releaseRepository = releaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateTenantApplicationReleaseDdlScriptsCommand request, CancellationToken cancellationToken)
    {
        var release = await _releaseRepository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Release not found."));

        if (release.AppDefinitionId != request.TenantApplicationId)
            return Result.Failure(Error.Validation("TenantApplication.ReleaseMismatch", "Release does not belong to this tenant application."));

        release.SetDdlScripts(request.DdlScriptsJson);
        _releaseRepository.Update(release);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
