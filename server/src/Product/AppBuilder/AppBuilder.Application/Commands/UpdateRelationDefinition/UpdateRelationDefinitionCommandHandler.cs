using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateRelationDefinition;

public sealed class UpdateRelationDefinitionCommandHandler
    : IApplicationRequestHandler<UpdateRelationDefinitionCommand, Result>
{
    private readonly IRelationDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public UpdateRelationDefinitionCommandHandler(
        IRelationDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateRelationDefinitionCommand request, CancellationToken cancellationToken)
    {
        var relation = await _repository.GetByIdAsync(request.RelationId, cancellationToken);
        if (relation == null)
            return Result.Failure(Error.NotFound("AppBuilder.RelationNotFound", "Relation definition not found."));
        relation.UpdateCascadeDelete(request.Request.CascadeDelete);
        _repository.Update(relation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
