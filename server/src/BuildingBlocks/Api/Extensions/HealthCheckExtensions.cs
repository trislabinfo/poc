using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace BuildingBlocks.Web.Extensions;

public static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddBuildingBlocksHealthChecks(this WebApplicationBuilder builder)
    {
        var services = builder.Services.AddHealthChecks();

        var dbConnectionString = builder.Configuration.GetConnectionString("Database");
        if (!string.IsNullOrWhiteSpace(dbConnectionString))
        {
            services.AddNpgSql(
                dbConnectionString,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "ready" });
        }

        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "cache", "ready" });
        }

        // "self" / live check is registered by ServiceDefaults.AddDefaultHealthChecks when using AddServiceDefaults()

        return builder;
    }

    public static WebApplication MapBuildingBlocksHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponse,
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponse,
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponse,
        });

        return app;
    }

    private static async Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
            }),
        });
        await context.Response.WriteAsync(result);
    }
}
