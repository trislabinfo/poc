namespace TenantApplication.Application.Services;

/// <summary>
/// Snapshot data from a platform (AppBuilder) release for copy-on-install.
/// </summary>
/// <param name="EntityJson">Entities + Relations JSON; null or empty if none.</param>
/// <param name="NavigationJson">Navigation definitions JSON (array); null or empty if none.</param>
public sealed record PlatformReleaseSnapshotDto(string? EntityJson, string? NavigationJson);

/// <summary>
/// Provides snapshot JSON (entities, navigation) from a platform (AppBuilder) release.
/// Used when installing an application to copy definitions into the tenant.
/// Implementations: in-process (same host as AppBuilder) or HTTP (microservice topology).
/// </summary>
public interface IPlatformReleaseSnapshotProvider
{
    /// <summary>
    /// Gets the snapshot (entity + navigation JSON) for the given platform release,
    /// or null if the release is not found.
    /// </summary>
    Task<PlatformReleaseSnapshotDto?> GetSnapshotAsync(Guid platformReleaseId, CancellationToken cancellationToken = default);
}
