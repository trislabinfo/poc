using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for entity definition (AppBuilder and TenantApplication).</summary>
public static class EntityDefinitionMapper
{
    public static EntityDefinitionDto ToDto(EntityDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new EntityDefinitionDto(
            entity.Id,
            entity.AppDefinitionId,
            entity.Name,
            entity.DisplayName,
            entity.Description,
            entity.AttributesJson,
            entity.PrimaryKey,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
