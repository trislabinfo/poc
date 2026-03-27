using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeletePropertyDefinition;

public sealed class DeletePropertyDefinitionCommandHandler
    : IApplicationRequestHandler<DeletePropertyDefinitionCommand, Result>
{
    private readonly IPropertyDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeletePropertyDefinitionCommandHandler(
        IPropertyDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeletePropertyDefinitionCommand request, CancellationToken cancellationToken)
    {
        var prop = await _repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (prop == null)
            return Result.Failure(Error.NotFound("AppBuilder.PropertyNotFound", "Property definition not found."));
        _repository.Delete(prop);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
