using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreatePropertyDefinition;

public sealed class CreatePropertyDefinitionCommandHandler
    : IApplicationRequestHandler<CreatePropertyDefinitionCommand, Result<Guid>>
{
    private readonly IPropertyDefinitionRepository _repository;
    private readonly IEntityDefinitionRepository _entityRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePropertyDefinitionCommandHandler(
        IPropertyDefinitionRepository repository,
        IEntityDefinitionRepository entityRepository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _entityRepository = entityRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var entity = await _entityRepository.GetByIdAsync(r.EntityDefinitionId, cancellationToken);
        if (entity == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.EntityNotFound", "Entity definition not found."));

        var existing = await _repository.GetByNameAsync(r.EntityDefinitionId, r.Name.Trim(), cancellationToken);
        if (existing != null)
            return Result<Guid>.Failure(Error.Conflict("AppBuilder.PropertyNameExists", "A property with this name already exists for this entity."));

        var result = PropertyDefinition.Create(
            r.EntityDefinitionId,
            r.Name,
            r.DisplayName,
            r.DataType,
            r.IsRequired,
            r.Order,
            _dateTimeProvider);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        var prop = result.Value;
        if (r.DefaultValue != null || (r.ValidationRulesJson != null && r.ValidationRulesJson != "{}"))
        {
            var updateResult = prop.Update(prop.DisplayName, prop.IsRequired, r.DefaultValue, r.ValidationRulesJson ?? "{}", prop.Order, _dateTimeProvider);
            if (updateResult.IsFailure) return Result<Guid>.Failure(updateResult.Error);
        }
        await _repository.AddAsync(prop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(prop.Id);
    }
}
