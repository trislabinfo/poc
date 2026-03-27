using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Application.BackgroundJobs;
using BuildingBlocks.Application.ErrorTracking;
using BuildingBlocks.Application.FeatureFlags;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Infrastructure.Auditing;
using BuildingBlocks.Infrastructure.BackgroundJobs;
using BuildingBlocks.Infrastructure.ErrorTracking;
using BuildingBlocks.Infrastructure.FeatureFlags;
using BuildingBlocks.Infrastructure.UnitOfWork;
using BuildingBlocks.Kernel.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers default BuildingBlocks implementations (no-op/vendor-agnostic): feature flags, auditing, error tracker, unit of work, background job scheduler, date/time provider.
    /// Does NOT register Hangfire or Sentry; hosts opt in via capability extensions (e.g. AddHangfireBackgroundJobs, AddSentryErrorTracking).
    /// </summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlagService, InMemoryFeatureFlagService>();
        services.AddScoped<ISecurityEventLogger, DatabaseSecurityEventLogger>();
        services.AddSingleton<IErrorTracker, NullErrorTracker>();
        services.AddScoped<IUnitOfWork, NullUnitOfWork>();
        services.AddSingleton<IBackgroundJobScheduler, NullBackgroundJobScheduler>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
