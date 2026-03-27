using Identity.Application;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Identity Infrastructure services (DbContext, repositories, domain services, Unit of Work).
    /// Schema is supplied by the module (e.g. IdentityModule.SchemaName). Uses ConnectionStrings:DefaultConnection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="schemaName">Schema name for the Identity module (unused; schema is on IdentityDbContext).</param>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string schemaName)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("dr-development-db")
            ?? configuration.GetConnectionString("dr-development")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection (or Aspire-injected dr-development-db) is required.");

        // Migrations are run outside the app (e.g. FluentMigrator); EF is used for CRUD only. Schema from IdentityDbContext.SchemaName.
        services.AddDbContext<IdentityDbContext>(options =>
        {
            // For simplicity, disable EF Core's built-in retry execution strategy.
            // The UnitOfWork/TransactionBehavior uses explicit transactions; combining those
            // with NpgsqlRetryingExecutionStrategy requires a custom execution wrapper.
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
