namespace TenantApplication.Application.Services;

/// <summary>
/// Copies entity, property, and relation definitions from a platform release snapshot
/// into the tenant application's definition tables so that tenant releases can generate DDL.
/// </summary>
public interface ICopyDefinitionsFromPlatformReleaseService
{
    /// <summary>
    /// Copies definitions from the platform release into the tenant application.
    /// Ids are remapped so that tenant definitions reference the new tenant application.
    /// </summary>
    /// <param name="tenantApplicationId">The installed tenant application id (target).</param>
    /// <param name="platformReleaseId">The platform release id (source).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success if copy completed; failure if snapshot not found or invalid.</returns>
    Task<BuildingBlocks.Kernel.Results.Result> CopyAsync(
        Guid tenantApplicationId,
        Guid platformReleaseId,
        CancellationToken cancellationToken = default);
}
