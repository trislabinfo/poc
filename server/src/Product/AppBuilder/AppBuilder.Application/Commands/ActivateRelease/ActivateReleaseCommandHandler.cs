using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.ActivateRelease;

public sealed class ActivateReleaseCommandHandler
    : IApplicationRequestHandler<ActivateReleaseCommand, Result>
{
    private readonly IApplicationReleaseRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public ActivateReleaseCommandHandler(
        IApplicationReleaseRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(ActivateReleaseCommand request, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("AppBuilder.ReleaseNotFound", "Release not found."));
        var appReleases = await _repository.GetByAppDefinitionIdAsync(release.AppDefinitionId, cancellationToken);
        foreach (var r in appReleases)
        {
            if (r.IsActive)
            {
                r.Deactivate();
                _repository.Update(r);
            }
        }
        release.Activate();
        _repository.Update(release);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
