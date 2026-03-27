using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AppDefinition.Application;

/// <summary>Registers shared AppDefinition.Application services (validators). Mappers are static; no registration needed.</summary>
/// <remarks>Schema derivation and migration services are registered via Capabilities.DatabaseSchema.</remarks>
public static class AppDefinitionApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAppDefinitionApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Validators.CreateEntityRequestValidator>();

        // Schema derivation and migration services are now in Capabilities.DatabaseSchema
        // Registered via services.AddDatabaseSchema() in module registration

        return services;
    }
}
