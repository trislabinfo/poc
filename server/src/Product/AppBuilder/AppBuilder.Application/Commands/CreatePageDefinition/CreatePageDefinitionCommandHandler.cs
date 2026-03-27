using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreatePageDefinition;

public sealed class CreatePageDefinitionCommandHandler
    : IApplicationRequestHandler<CreatePageDefinitionCommand, Result<Guid>>
{
    private readonly IPageDefinitionRepository _repository;
    private readonly IAppDefinitionRepository _appRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageDefinitionCommandHandler(
        IPageDefinitionRepository repository,
        IAppDefinitionRepository appRepository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _appRepository = appRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePageDefinitionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var app = await _appRepository.GetByIdAsync(r.AppDefinitionId, cancellationToken);
        if (app == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.ApplicationNotFound", "Application definition not found."));

        var result = PageDefinition.Create(
            r.AppDefinitionId,
            r.Name,
            r.Route,
            r.ConfigurationJson,
            _dateTimeProvider);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        var page = result.Value;
        await _repository.AddAsync(page, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(page.Id);
    }
}
