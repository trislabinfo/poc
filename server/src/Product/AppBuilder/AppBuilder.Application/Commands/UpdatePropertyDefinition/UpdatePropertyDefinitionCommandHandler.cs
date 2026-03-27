using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdatePropertyDefinition;

public sealed class UpdatePropertyDefinitionCommandHandler
    : IApplicationRequestHandler<UpdatePropertyDefinitionCommand, Result>
{
    private readonly IPropertyDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePropertyDefinitionCommandHandler(
        IPropertyDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdatePropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var prop = await _repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (prop == null)
            return Result.Failure(Error.NotFound("AppBuilder.PropertyNotFound", "Property definition not found."));
        var r = request.Request;
        var result = prop.Update(r.DisplayName, r.IsRequired, r.DefaultValue, r.ValidationRulesJson, r.Order, _dateTimeProvider);
        if (result.IsFailure) return result;
        _repository.Update(prop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
