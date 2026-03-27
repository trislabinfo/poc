using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenant.Application;
using Tenant.Domain.Repositories;
using Tenant.Infrastructure.Data;
using Tenant.Infrastructure.Repositories;

namespace Tenant.Infrastructure;

public static class TenantInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Tenant Infrastructure services (DbContext, repository, Unit of Work).
    /// Schema is supplied by the module (e.g. TenantModule.SchemaName). Uses ConnectionStrings:DefaultConnection.
    /// </summary>
    public static IServiceCollection AddTenantInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string schemaName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        // DefaultConnection for standalone; dr-development-db / dr-development when run under Aspire AppHost
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("dr-development-db")
            ?? configuration.GetConnectionString("dr-development")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection (or Aspire-injected dr-development-db) is required.");

        services.AddDbContext<TenantDbContext>(options =>
        {
            // For simplicity, disable EF Core's built-in retry execution strategy.
            // The UnitOfWork/TransactionBehavior uses explicit transactions; combining those
            // with NpgsqlRetryingExecutionStrategy requires a custom execution wrapper.
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITenantUnitOfWork, TenantUnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantUserRepository, TenantUserRepository>();

        return services;
    }
}
