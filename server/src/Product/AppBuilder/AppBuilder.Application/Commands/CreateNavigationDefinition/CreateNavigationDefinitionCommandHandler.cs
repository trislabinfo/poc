using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateNavigationDefinition;

public sealed class CreateNavigationDefinitionCommandHandler
    : IApplicationRequestHandler<CreateNavigationDefinitionCommand, Result<Guid>>
{
    private readonly INavigationDefinitionRepository _repository;
    private readonly IAppDefinitionRepository _appRepository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateNavigationDefinitionCommandHandler(
        INavigationDefinitionRepository repository,
        IAppDefinitionRepository appRepository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _appRepository = appRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateNavigationDefinitionCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var app = await _appRepository.GetByIdAsync(r.AppDefinitionId, cancellationToken);
        if (app == null)
            return Result<Guid>.Failure(Error.NotFound("AppBuilder.ApplicationNotFound", "Application definition not found."));

        var result = NavigationDefinition.Create(
            r.AppDefinitionId,
            r.Name,
            r.ConfigurationJson,
            _dateTimeProvider);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        var nav = result.Value;
        await _repository.AddAsync(nav, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(nav.Id);
    }
}
