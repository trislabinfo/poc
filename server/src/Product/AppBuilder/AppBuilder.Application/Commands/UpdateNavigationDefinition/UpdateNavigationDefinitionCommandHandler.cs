using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateNavigationDefinition;

public sealed class UpdateNavigationDefinitionCommandHandler
    : IApplicationRequestHandler<UpdateNavigationDefinitionCommand, Result>
{
    private readonly INavigationDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateNavigationDefinitionCommandHandler(
        INavigationDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdateNavigationDefinitionCommand request, CancellationToken cancellationToken)
    {
        var nav = await _repository.GetByIdAsync(request.NavigationId, cancellationToken);
        if (nav == null)
            return Result.Failure(Error.NotFound("AppBuilder.NavigationNotFound", "Navigation definition not found."));
        var r = request.Request;
        var result = nav.Update(r.Name, r.ConfigurationJson, _dateTimeProvider);
        if (result.IsFailure) return result;
        _repository.Update(nav);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
