using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for page definition (AppBuilder and TenantApplication).</summary>
public static class PageDefinitionMapper
{
    public static PageDefinitionDto ToDto(PageDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new PageDefinitionDto(
            entity.Id,
            entity.AppDefinitionId,
            entity.Name,
            entity.Route,
            entity.ConfigurationJson,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
