using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Queries.GetReleaseEntityViewHtml;

public sealed class GetReleaseEntityViewHtmlQueryHandler
    : IApplicationRequestHandler<GetReleaseEntityViewHtmlQuery, Result<string?>>
{
    private readonly IReleaseEntityViewRepository _repository;

    public GetReleaseEntityViewHtmlQueryHandler(IReleaseEntityViewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<string?>> HandleAsync(
        GetReleaseEntityViewHtmlQuery request,
        CancellationToken cancellationToken)
    {
        var view = await _repository.GetAsync(
            request.ReleaseId,
            request.EntityId,
            request.ViewType,
            cancellationToken);
        if (view == null)
            return Result<string?>.Failure(Error.NotFound("TenantApplication.EntityViewNotFound", "Entity view not found."));
        return Result<string?>.Success(view.Html);
    }
}
