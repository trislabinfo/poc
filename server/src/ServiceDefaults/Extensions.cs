using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

internal static class HealthEndpointPath
{
    public const string Path = "/health";
}

internal static class AlivenessEndpointPath
{
    public const string Path = "/alive";
}

/// <summary>
/// Aspire service defaults: OpenTelemetry, health checks, service discovery, HTTP resilience.
/// </summary>
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Suppress MediatR (Lucky Penny) license warning in Development; app is not used in production.
        if (builder.Environment.IsDevelopment())
            builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);

        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var useOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (useOtlp)
                    metrics.AddOtlpExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(aspNetCore =>
                    {
                        aspNetCore.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath.Path)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath.Path);
                    })
                    .AddHttpClientInstrumentation();
                if (useOtlp)
                    tracing.AddOtlpExporter();
            });

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }

    /// <summary>
    /// Maps /health and /alive endpoints (only in Development).
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath.Path);
            app.MapHealthChecks(AlivenessEndpointPath.Path, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }
        return app;
    }
}
