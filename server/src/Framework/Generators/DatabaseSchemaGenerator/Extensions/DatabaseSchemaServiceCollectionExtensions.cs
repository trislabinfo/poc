using Capabilities.DatabaseSchema.Abstractions;
using Capabilities.DatabaseSchema.EfCore;
using Microsoft.Extensions.DependencyInjection;

namespace Capabilities.DatabaseSchema.Extensions;

/// <summary>Service registration extensions for DatabaseSchema capability.</summary>
public static class DatabaseSchemaServiceCollectionExtensions
{
    /// <summary>
    /// Registers DatabaseSchema services (schema derivation, comparison, DDL generation, etc.).
    /// </summary>
    public static IServiceCollection AddDatabaseSchema(this IServiceCollection services)
    {
        // Register EF Core-based implementations
        services.AddScoped<ISchemaDeriver, EfCoreSchemaDeriver>();
        services.AddScoped<ISchemaComparer, EfCoreSchemaComparer>();
        services.AddScoped<IDdlScriptGenerator, EfCoreDdlScriptGenerator>();
        services.AddScoped<IDatabaseSchemaReader, EfCoreDatabaseSchemaReader>();
        services.AddScoped<IMigrationScriptGenerator, EfCoreMigrationScriptGenerator>();

        return services;
    }
}
