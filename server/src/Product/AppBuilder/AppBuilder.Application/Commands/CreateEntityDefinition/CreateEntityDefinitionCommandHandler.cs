using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateEntityDefinition;

public sealed class CreateEntityDefinitionCommandHandler
    : IApplicationRequestHandler<CreateEntityDefinitionCommand, Result<Guid>>
{
    private readonly IEntityDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateEntityDefinitionCommandHandler(
        IEntityDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateEntityDefinitionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var entityResult = EntityDefinition.Create(
            r.AppDefinitionId,
            r.Name,
            r.DisplayName,
            _dateTimeProvider,
            r.Description,
            r.AttributesJson,
            r.PrimaryKey);
        if (entityResult.IsFailure) return Result<Guid>.Failure(entityResult.Error);
        var entity = entityResult.Value;
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}
