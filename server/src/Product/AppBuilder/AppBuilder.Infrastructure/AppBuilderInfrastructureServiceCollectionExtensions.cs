using AppBuilder.Application;
using AppBuilder.Domain.Repositories;
using AppBuilder.Infrastructure.Data;
using AppBuilder.Infrastructure.Repositories;
using AppDefinition.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBuilder.Infrastructure;

public static class AppBuilderInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers AppBuilder Infrastructure services (DbContext, repositories, Unit of Work).
    /// Schema is supplied by the module. Uses ConnectionStrings:DefaultConnection.
    /// </summary>
    public static IServiceCollection AddAppBuilderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string schemaName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("dr-development-db")
            ?? configuration.GetConnectionString("dr-development")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection (or Aspire-injected dr-development-db) is required.");

        services.AddDbContext<AppBuilderDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAppBuilderUnitOfWork, AppBuilderUnitOfWork>();
        services.AddScoped<IAppDefinitionRepository, AppDefinitionRepository>();
        services.AddScoped<IApplicationReleaseRepository, ApplicationReleaseRepository>();
        services.AddScoped<IEntityDefinitionRepository, EntityDefinitionRepository>();
        services.AddScoped<IPropertyDefinitionRepository, PropertyDefinitionRepository>();
        services.AddScoped<IRelationDefinitionRepository, RelationDefinitionRepository>();
        services.AddScoped<INavigationDefinitionRepository, NavigationDefinitionRepository>();
        services.AddScoped<IPageDefinitionRepository, PageDefinitionRepository>();
        services.AddScoped<IDataSourceDefinitionRepository, DataSourceDefinitionRepository>();
        services.AddScoped<IReleaseEntityViewRepository, ReleaseEntityViewRepository>();

        return services;
    }
}
