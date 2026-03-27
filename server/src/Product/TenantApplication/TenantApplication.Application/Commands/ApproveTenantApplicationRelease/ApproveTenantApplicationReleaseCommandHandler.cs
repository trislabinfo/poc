using AppDefinition.Domain.Repositories;
using AppDefinition.HtmlGeneration;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.ApproveTenantApplicationRelease;

public sealed class ApproveTenantApplicationReleaseCommandHandler
    : IApplicationRequestHandler<ApproveTenantApplicationReleaseCommand, Result>
{
    private readonly ITenantApplicationReleaseRepository _releaseRepository;
    private readonly IReleaseEntityViewRepository _releaseEntityViews;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveTenantApplicationReleaseCommandHandler(
        ITenantApplicationReleaseRepository releaseRepository,
        IReleaseEntityViewRepository releaseEntityViews,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _releaseRepository = releaseRepository;
        _releaseEntityViews = releaseEntityViews;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(ApproveTenantApplicationReleaseCommand request, CancellationToken cancellationToken)
    {
        var release = await _releaseRepository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("TenantApplication.ReleaseNotFound", "Release not found."));

        if (release.AppDefinitionId != request.TenantApplicationId)
            return Result.Failure(Error.Validation("TenantApplication.ReleaseMismatch", "Release does not belong to this tenant application."));

        var result = release.ApproveDdlScripts(request.ApprovedBy, _dateTimeProvider);
        if (result.IsFailure)
            return result;

        // Generate and set initial view HTML (plan Step 5)
        var initialHtml = InitialViewComposer.Compose(release.NavigationJson, release.PageJson);
        if (initialHtml == null)
            return Result.Failure(Error.Validation("TenantApplication.HtmlGenerationFailed", "Initial view HTML generation failed."));
        release.SetInitialViewHtml(initialHtml);

        // Generate and persist entity views (list + form per entity)
        var entityViews = EntityViewsComposer.Compose(release.EntityJson, release.Id);
        await _releaseEntityViews.SetForReleaseAsync(release.Id, entityViews, cancellationToken);

        _releaseRepository.Update(release);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
