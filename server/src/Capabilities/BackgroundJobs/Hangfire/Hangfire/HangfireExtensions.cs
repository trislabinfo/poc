using BuildingBlocks.Application.BackgroundJobs;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Capabilities.BackgroundJobs.Hangfire;

public static class HangfireExtensions
{
    public static WebApplicationBuilder AddHangfireBackgroundJobs(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        builder.Services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                });
        });

        builder.Services.AddHangfireServer();

        builder.Services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        return builder;
    }

    /// <summary>
    /// Enables the Hangfire dashboard at /admin/jobs when Hangfire was configured (i.e. when AddHangfireBackgroundJobs had a Database connection string).
    /// No-op when Hangfire was not added.
    /// </summary>
    public static WebApplication UseHangfireDashboard(this WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return app;
        }

        app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()],
        });

        return app;
    }
}

internal sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
