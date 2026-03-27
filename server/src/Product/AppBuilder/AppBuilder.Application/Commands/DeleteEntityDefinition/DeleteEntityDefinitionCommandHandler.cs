using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteEntityDefinition;

public sealed class DeleteEntityDefinitionCommandHandler
    : IApplicationRequestHandler<DeleteEntityDefinitionCommand, Result>
{
    private readonly IEntityDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeleteEntityDefinitionCommandHandler(
        IEntityDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteEntityDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.EntityId, cancellationToken);
        if (entity == null)
            return Result.Failure(Error.NotFound("AppBuilder.EntityNotFound", "Entity definition not found."));
        _repository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
