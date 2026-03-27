using AppDefinition.Domain.Repositories;
using TenantApplication.Application.Services;

namespace TenantApplication.Infrastructure.Services;

/// <summary>
/// Reads the snapshot (entity + navigation JSON) from a platform (AppBuilder) release.
/// Uses the shared <see cref="IApplicationReleaseRepository"/> (implemented by AppBuilder in the same host).
/// </summary>
public sealed class PlatformReleaseSnapshotProvider : IPlatformReleaseSnapshotProvider
{
    private readonly IApplicationReleaseRepository _releaseRepository;

    public PlatformReleaseSnapshotProvider(IApplicationReleaseRepository releaseRepository)
    {
        _releaseRepository = releaseRepository;
    }

    /// <inheritdoc />
    public async Task<PlatformReleaseSnapshotDto?> GetSnapshotAsync(Guid platformReleaseId, CancellationToken cancellationToken = default)
    {
        var release = await _releaseRepository.GetByIdAsync(platformReleaseId, cancellationToken);
        if (release == null)
            return null;
        var entityJson = string.IsNullOrWhiteSpace(release.EntityJson) || release.EntityJson == "{}" ? null : release.EntityJson;
        var navigationJson = string.IsNullOrWhiteSpace(release.NavigationJson) || release.NavigationJson == "[]" ? null : release.NavigationJson;
        return new PlatformReleaseSnapshotDto(entityJson, navigationJson);
    }
}
