using AppDefinition.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TenantApplication.Application;
using TenantApplication.Application.Services;
using TenantApplication.Domain.DatabaseProvisioning;
using TenantApplication.Domain.Repositories;
using TenantApplication.Infrastructure.Data;
using TenantApplication.Infrastructure.DatabaseProvisioning;
using TenantApplication.Infrastructure.Repositories;
using TenantApplication.Infrastructure.Services;

namespace TenantApplication.Infrastructure;

public static class TenantApplicationInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers TenantApplication Infrastructure (tenantapplication schema: TenantApplication, definition tables, repos).
    /// Uses ConnectionStrings:DefaultConnection for standalone; Aspire injects dr-development-db / dr-development when run from AppHost.
    /// </summary>
    public static IServiceCollection AddTenantApplicationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("dr-development-db")
            ?? configuration.GetConnectionString("dr-development")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection (or Aspire-injected dr-development-db) is required.");

        services.AddDbContext<TenantApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITenantApplicationUnitOfWork, TenantApplicationUnitOfWork>();
        services.AddScoped<ITenantApplicationRepository, TenantApplicationRepository>();
        services.AddScoped<ITenantApplicationEnvironmentRepository, TenantApplicationEnvironmentRepository>();
        services.AddScoped<ITenantApplicationMigrationRepository, TenantApplicationMigrationRepository>();
        services.AddScoped<ITenantApplicationReleaseRepository, TenantApplicationReleaseRepository>();
        services.AddScoped<IReleaseEntityViewRepository, ReleaseEntityViewRepository>();
        services.AddScoped<ITenantDefinitionSnapshotReader, TenantDefinitionSnapshotReader>();
        services.AddScoped<IDatabaseProvisioner, DatabaseProvisioner>();
        services.AddScoped<ITenantEnvironmentConnectionStringProvider, TenantEnvironmentConnectionStringProvider>();

        var appBuilderBaseUrl = configuration["AppBuilder:BaseUrl"]?.Trim();
        if (!string.IsNullOrEmpty(appBuilderBaseUrl))
        {
            services.Configure<AppBuilderClientOptions>(configuration.GetSection(AppBuilderClientOptions.SectionName));
            services.AddHttpClient<HttpPlatformReleaseSnapshotProvider>(client =>
            {
                client.BaseAddress = new Uri(appBuilderBaseUrl.TrimEnd('/') + "/");
            });
            services.AddScoped<IPlatformReleaseSnapshotProvider>(sp => sp.GetRequiredService<HttpPlatformReleaseSnapshotProvider>());
        }
        else
        {
            services.AddScoped<IPlatformReleaseSnapshotProvider, PlatformReleaseSnapshotProvider>();
        }

        services.AddScoped<ICopyDefinitionsFromPlatformReleaseService, CopyDefinitionsFromPlatformReleaseService>();

        return services;
    }
}
