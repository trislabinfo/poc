namespace BuildingBlocks.Application.FeatureFlags;

/// <summary>
/// Abstraction over a feature flag provider.
/// </summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsEnabledForTenantAsync(string featureName, Guid tenantId, CancellationToken cancellationToken = default);
}

