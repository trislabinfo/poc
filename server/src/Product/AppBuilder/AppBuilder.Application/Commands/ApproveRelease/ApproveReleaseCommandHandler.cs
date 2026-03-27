using AppDefinition.Domain.Repositories;
using AppDefinition.HtmlGeneration;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.ApproveRelease;

public sealed class ApproveReleaseCommandHandler
    : IApplicationRequestHandler<ApproveReleaseCommand, Result>
{
    private readonly IApplicationReleaseRepository _releaseRepository;
    private readonly IReleaseEntityViewRepository _releaseEntityViews;
    private readonly IAppBuilderUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveReleaseCommandHandler(
        IApplicationReleaseRepository releaseRepository,
        IReleaseEntityViewRepository releaseEntityViews,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _releaseRepository = releaseRepository;
        _releaseEntityViews = releaseEntityViews;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(ApproveReleaseCommand request, CancellationToken cancellationToken)
    {
        var release = await _releaseRepository.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release == null)
            return Result.Failure(Error.NotFound("AppBuilder.ReleaseNotFound", "Release not found."));

        if (release.AppDefinitionId != request.AppDefinitionId)
            return Result.Failure(Error.Validation("AppBuilder.ReleaseMismatch", "Release does not belong to this application."));

        var result = release.ApproveDdlScripts(request.ApprovedBy, _dateTimeProvider);
        if (result.IsFailure)
            return result;

        // Generate and set initial view HTML (plan Step 4)
        var initialHtml = InitialViewComposer.Compose(release.NavigationJson, release.PageJson);
        if (initialHtml == null)
            return Result.Failure(Error.Validation("AppBuilder.HtmlGenerationFailed", "Initial view HTML generation failed."));
        release.SetInitialViewHtml(initialHtml);

        // Generate and persist entity views (list + form per entity)
        var entityViews = EntityViewsComposer.Compose(release.EntityJson, release.Id);
        await _releaseEntityViews.SetForReleaseAsync(release.Id, entityViews, cancellationToken);

        _releaseRepository.Update(release);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
