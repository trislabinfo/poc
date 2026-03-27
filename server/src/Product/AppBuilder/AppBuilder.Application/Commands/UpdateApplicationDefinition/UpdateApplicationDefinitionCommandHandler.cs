using AppBuilder.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdateAppDefinition;

public sealed class UpdateAppDefinitionCommandHandler
    : IApplicationRequestHandler<UpdateAppDefinitionCommand, Result>
{
    private readonly IAppDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateAppDefinitionCommandHandler(
        IAppDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdateAppDefinitionCommand request, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (app == null)
            return Result.Failure(Error.NotFound("AppBuilder.ApplicationNotFound", "Application definition not found."));
        var result = app.Update(request.Name, request.Description, _dateTimeProvider);
        if (result.IsFailure) return result;
        _repository.Update(app);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
