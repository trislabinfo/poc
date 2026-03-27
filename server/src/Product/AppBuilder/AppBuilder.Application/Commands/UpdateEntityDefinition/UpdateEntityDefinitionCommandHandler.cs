using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateEntityDefinition;

public sealed class UpdateEntityDefinitionCommandHandler
    : IApplicationRequestHandler<UpdateEntityDefinitionCommand, Result>
{
    private readonly IEntityDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEntityDefinitionCommandHandler(
        IEntityDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdateEntityDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.EntityId, cancellationToken);
        if (entity == null)
            return Result.Failure(Error.NotFound("AppBuilder.EntityNotFound", "Entity definition not found."));
        var r = request.Request;
        var result = entity.Update(r.Name, r.DisplayName, r.Description, r.AttributesJson, r.PrimaryKey, _dateTimeProvider);
        if (result.IsFailure) return result;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
