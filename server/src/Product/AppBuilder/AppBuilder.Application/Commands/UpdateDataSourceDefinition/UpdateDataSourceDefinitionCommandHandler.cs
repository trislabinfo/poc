using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateDataSourceDefinition;

public sealed class UpdateDataSourceDefinitionCommandHandler
    : IApplicationRequestHandler<UpdateDataSourceDefinitionCommand, Result>
{
    private readonly IDataSourceDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateDataSourceDefinitionCommandHandler(
        IDataSourceDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdateDataSourceDefinitionCommand request, CancellationToken cancellationToken)
    {
        var dataSource = await _repository.GetByIdAsync(request.DataSourceId, cancellationToken);
        if (dataSource == null)
            return Result.Failure(Error.NotFound("AppBuilder.DataSourceNotFound", "Data source definition not found."));
        var r = request.Request;
        var result = dataSource.Update(r.Name, r.ConfigurationJson, _dateTimeProvider);
        if (result.IsFailure) return result;
        _repository.Update(dataSource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
