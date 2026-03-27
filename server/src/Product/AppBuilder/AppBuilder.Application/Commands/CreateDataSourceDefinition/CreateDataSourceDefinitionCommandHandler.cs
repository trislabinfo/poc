using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateDataSourceDefinition;

public sealed class CreateDataSourceDefinitionCommandHandler
    : IApplicationRequestHandler<CreateDataSourceDefinitionCommand, Result<Guid>>
{
    private readonly IDataSourceDefinitionRepository _repository;
    private readonly IAppDefinitionRepository _appRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateDataSourceDefinitionCommandHandler(
        IDataSourceDefinitionRepository repository,
        IAppDefinitionRepository appRepository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _appRepository = appRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateDataSourceDefinitionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var app = await _appRepository.GetByIdAsync(r.AppDefinitionId, cancellationToken);
        if (app == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.ApplicationNotFound", "Application definition not found."));

        var result = DataSourceDefinition.Create(
            r.AppDefinitionId,
            r.Name,
            r.Type,
            r.ConfigurationJson,
            _dateTimeProvider);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        var dataSource = result.Value;
        await _repository.AddAsync(dataSource, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(dataSource.Id);
    }
}
