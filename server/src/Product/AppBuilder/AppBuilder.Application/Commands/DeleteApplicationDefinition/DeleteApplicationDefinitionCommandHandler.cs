using AppBuilder.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteAppDefinition;

public sealed class DeleteAppDefinitionCommandHandler
    : IApplicationRequestHandler<DeleteAppDefinitionCommand, Result>
{
    private readonly IAppDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeleteAppDefinitionCommandHandler(
        IAppDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteAppDefinitionCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (app == null)
            return Result.Failure(Error.NotFound("AppBuilder.ApplicationNotFound", "Application definition not found."));
        if (app.Status != AppBuilder.Domain.Enums.ApplicationStatus.Draft)
            return Result.Failure(Error.Validation("AppBuilder.InvalidStatus", "Only draft applications can be deleted."));
        _repository.Delete(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
