using BuildingBlocks.Application.FeatureFlags;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// Simple in-memory implementation of <see cref="IFeatureFlagService"/>.
/// Reads flags from configuration: FeatureFlags:{FlagName}.
/// </summary>
internal sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public InMemoryFeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var value = _configuration[$"FeatureFlags:{featureName}"];
        return Task.FromResult(bool.TryParse(value, out var enabled) && enabled);
    }

    public Task<bool> IsEnabledForUserAsync(
        string featureName,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as global flag.
        return IsEnabledAsync(featureName, cancellationToken);
    }

    public Task<bool> IsEnabledForTenantAsync(
        string featureName,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation: same as global flag.
        return IsEnabledAsync(featureName, cancellationToken);
    }
}

