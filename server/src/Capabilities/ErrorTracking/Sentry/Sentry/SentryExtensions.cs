using BuildingBlocks.Application.ErrorTracking;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Capabilities.ErrorTracking.Sentry;

public static class SentryExtensions
{
    public static WebApplicationBuilder AddSentryErrorTracking(this WebApplicationBuilder builder)
    {
        var dsn = builder.Configuration["Sentry:Dsn"];
        if (!string.IsNullOrWhiteSpace(dsn))
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = dsn;
                options.TracesSampleRate = 1.0;
                options.Environment = builder.Environment.EnvironmentName;
            });
        }

        builder.Services.AddSingleton<IErrorTracker, SentryErrorTracker>();

        return builder;
    }
}
