using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeletePageDefinition;

public sealed class DeletePageDefinitionCommandHandler
    : IApplicationRequestHandler<DeletePageDefinitionCommand, Result>
{
    private readonly IPageDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeletePageDefinitionCommandHandler(
        IPageDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeletePageDefinitionCommand request, CancellationToken cancellationToken)
    {
        var page = await _repository.GetByIdAsync(request.PageId, cancellationToken);
        if (page == null)
            return Result.Failure(Error.NotFound("AppBuilder.PageNotFound", "Page definition not found."));
        _repository.Delete(page);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
