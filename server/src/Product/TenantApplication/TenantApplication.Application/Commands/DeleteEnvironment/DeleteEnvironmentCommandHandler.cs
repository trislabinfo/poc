using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.DeleteEnvironment;

public sealed class DeleteEnvironmentCommandHandler
    : IApplicationRequestHandler<DeleteEnvironmentCommand, Result>
{
    private readonly ITenantApplicationRepository _appRepository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;

    public DeleteEnvironmentCommandHandler(
        ITenantApplicationRepository appRepository,
        ITenantApplicationUnitOfWork unitOfWork)
    {
        _appRepository = appRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var app = await _appRepository.GetByIdAsync(request.TenantApplicationId, cancellationToken);
        if (app == null)
            return Result.Failure(Error.NotFound("TenantApplication.NotFound", "Tenant application not found."));
        var result = app.RemoveEnvironment(request.EnvironmentId);
        if (result.IsFailure) return result;
        _appRepository.Update(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
