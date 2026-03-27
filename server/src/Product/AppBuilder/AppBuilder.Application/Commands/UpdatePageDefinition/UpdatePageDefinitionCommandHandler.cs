using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.UpdatePageDefinition;

public sealed class UpdatePageDefinitionCommandHandler
    : IApplicationRequestHandler<UpdatePageDefinitionCommand, Result>
{
    private readonly IPageDefinitionRepository _repository;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePageDefinitionCommandHandler(
        IPageDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdatePageDefinitionCommand request, CancellationToken cancellationToken)
    {
        var page = await _repository.GetByIdAsync(request.PageId, cancellationToken);
        if (page == null)
            return Result.Failure(Error.NotFound("AppBuilder.PageNotFound", "Page definition not found."));
        var r = request.Request;
        var result = page.Update(r.Name, r.Route, r.ConfigurationJson, _dateTimeProvider);
        if (result.IsFailure) return result;
        _repository.Update(page);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
