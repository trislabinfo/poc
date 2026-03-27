using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateReleaseDdlScripts;

public sealed class UpdateReleaseDdlScriptsCommandHandler
    : IApplicationRequestHandler<UpdateReleaseDdlScriptsCommand, Result>
{
    private readonly IApplicationReleaseRepository _releaseRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public UpdateReleaseDdlScriptsCommandHandler(
        IApplicationReleaseRepository releaseRepository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _releaseRepository = releaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateReleaseDdlScriptsCommand request, CancellationToken cancellationToken)
    {
        var release = await _releaseRepository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("AppBuilder.ReleaseNotFound", "Release not found."));

        if (release.AppDefinitionId != request.AppDefinitionId)
            return Result.Failure(Error.Validation("AppBuilder.ReleaseMismatch", "Release does not belong to this application."));

        release.SetDdlScripts(request.DdlScriptsJson);
        _releaseRepository.Update(release);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
