using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteNavigationDefinition;

public sealed class DeleteNavigationDefinitionCommandHandler
    : IApplicationRequestHandler<DeleteNavigationDefinitionCommand, Result>
{
    private readonly INavigationDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeleteNavigationDefinitionCommandHandler(
        INavigationDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteNavigationDefinitionCommand request, CancellationToken cancellationToken)
    {
        var nav = await _repository.GetByIdAsync(request.NavigationId, cancellationToken);
        if (nav == null)
            return Result.Failure(Error.NotFound("AppBuilder.NavigationNotFound", "Navigation definition not found."));
        _repository.Delete(nav);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
