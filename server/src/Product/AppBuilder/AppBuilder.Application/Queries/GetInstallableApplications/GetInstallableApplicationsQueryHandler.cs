using AppBuilder.Application.DTOs;
using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetInstallableApplications;

public sealed class GetInstallableApplicationsQueryHandler
    : IApplicationRequestHandler<GetInstallableApplicationsQuery, Result<List<InstallableApplicationDto>>>
{
    private readonly IAppDefinitionRepository _appRepository;
    private readonly IApplicationReleaseRepository _releaseRepository;

    public GetInstallableApplicationsQueryHandler(
        IAppDefinitionRepository appRepository,
        IApplicationReleaseRepository releaseRepository)
    {
        _appRepository = appRepository;
        _releaseRepository = releaseRepository;
    }

    public async Task<Result<List<InstallableApplicationDto>>> HandleAsync(
        GetInstallableApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var allApps = await _appRepository.ListAsync(cancellationToken);
        var publicApps = allApps.Where(x => x.IsPublic).ToList();
        var result = new List<InstallableApplicationDto>();
        foreach (var app in publicApps)
        {
            var activeRelease = await _releaseRepository.GetActiveReleaseAsync(app.Id, cancellationToken);
            if (activeRelease == null) continue;
            result.Add(new InstallableApplicationDto(
                app.Id,
                app.Name,
                app.Slug,
                app.Description,
                activeRelease.Id,
                activeRelease.Version,
                activeRelease.Major,
                activeRelease.Minor,
                activeRelease.Patch,
                activeRelease.ReleaseNotes));
        }
        return Result<List<InstallableApplicationDto>>.Success(result);
    }
}
