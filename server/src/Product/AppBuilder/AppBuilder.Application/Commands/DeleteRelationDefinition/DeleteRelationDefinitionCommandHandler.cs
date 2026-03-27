using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteRelationDefinition;

public sealed class DeleteRelationDefinitionCommandHandler
    : IApplicationRequestHandler<DeleteRelationDefinitionCommand, Result>
{
    private readonly IRelationDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeleteRelationDefinitionCommandHandler(
        IRelationDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteRelationDefinitionCommand request, CancellationToken cancellationToken)
    {
        var relation = await _repository.GetByIdAsync(request.RelationId, cancellationToken);
        if (relation == null)
            return Result.Failure(Error.NotFound("AppBuilder.RelationNotFound", "Relation definition not found."));
        _repository.Delete(relation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
