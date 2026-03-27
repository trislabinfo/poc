using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateRelationDefinition;

public sealed class CreateRelationDefinitionCommandHandler
    : IApplicationRequestHandler<CreateRelationDefinitionCommand, Result<Guid>>
{
    private readonly IRelationDefinitionRepository _repository;
    private readonly IEntityDefinitionRepository _entityRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRelationDefinitionCommandHandler(
        IRelationDefinitionRepository repository,
        IEntityDefinitionRepository entityRepository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _entityRepository = entityRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateRelationDefinitionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var sourceEntity = await _entityRepository.GetByIdAsync(r.SourceEntityId, cancellationToken);
        if (sourceEntity == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.SourceEntityNotFound", "Source entity definition not found."));
        var targetEntity = await _entityRepository.GetByIdAsync(r.TargetEntityId, cancellationToken);
        if (targetEntity == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.TargetEntityNotFound", "Target entity definition not found."));

        var result = RelationDefinition.Create(
            r.SourceEntityId,
            r.TargetEntityId,
            r.Name,
            r.RelationType,
            r.CascadeDelete,
            _dateTimeProvider);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        var relation = result.Value;
        await _repository.AddAsync(relation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(relation.Id);
    }
}
