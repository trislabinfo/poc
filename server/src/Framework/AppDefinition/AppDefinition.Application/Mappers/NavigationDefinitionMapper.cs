using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for navigation definition (AppBuilder and TenantApplication).</summary>
public static class NavigationDefinitionMapper
{
    public static NavigationDefinitionDto ToDto(NavigationDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new NavigationDefinitionDto(
            entity.Id,
            entity.AppDefinitionId,
            entity.Name,
            entity.ConfigurationJson,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
