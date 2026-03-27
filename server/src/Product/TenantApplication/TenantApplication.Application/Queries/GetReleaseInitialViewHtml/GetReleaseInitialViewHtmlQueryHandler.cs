using AppDefinition.HtmlGeneration;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Queries.GetReleaseInitialViewHtml;

public sealed class GetReleaseInitialViewHtmlQueryHandler
    : IApplicationRequestHandler<GetReleaseInitialViewHtmlQuery, Result<string?>>
{
    private readonly ITenantApplicationReleaseRepository _repository;

    public GetReleaseInitialViewHtmlQueryHandler(ITenantApplicationReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<string?>> HandleAsync(
        GetReleaseInitialViewHtmlQuery request,
        CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result<string?>.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Release not found."));

        // Prefer stored HTML unless it's stale (nav divs without data-label); then regenerate from JSON
        if (!string.IsNullOrEmpty(release.InitialViewHtml) && !InitialViewComposer.IsStaleNavHtml(release.InitialViewHtml))
            return Result<string?>.Success(release.InitialViewHtml);

        var generated = InitialViewComposer.Compose(release.NavigationJson, release.PageJson);
        return Result<string?>.Success(generated);
    }
}
