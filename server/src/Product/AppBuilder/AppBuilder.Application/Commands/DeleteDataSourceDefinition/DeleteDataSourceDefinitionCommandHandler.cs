using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.DeleteDataSourceDefinition;

public sealed class DeleteDataSourceDefinitionCommandHandler
    : IApplicationRequestHandler<DeleteDataSourceDefinitionCommand, Result>
{
    private readonly IDataSourceDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;

    public DeleteDataSourceDefinitionCommandHandler(
        IDataSourceDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteDataSourceDefinitionCommand request, CancellationToken cancellationToken)
    {
        var dataSource = await _repository.GetByIdAsync(request.DataSourceId, cancellationToken);
        if (dataSource == null)
            return Result.Failure(Error.NotFound("AppBuilder.DataSourceNotFound", "Data source definition not found."));
        _repository.Delete(dataSource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
