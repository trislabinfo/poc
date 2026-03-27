using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeactivateRelease;

public sealed class DeactivateReleaseCommandHandler
    : IApplicationRequestHandler<DeactivateReleaseCommand, Result>
{
    private readonly IApplicationReleaseRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeactivateReleaseCommandHandler(
        IApplicationReleaseRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeactivateReleaseCommand request, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("AppBuilder.ReleaseNotFound", "Release not found."));
        release.Deactivate();
        _repository.Update(release);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
