using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for property definition (AppBuilder and TenantApplication).</summary>
public static class PropertyDefinitionMapper
{
    public static PropertyDefinitionDto ToDto(PropertyDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new PropertyDefinitionDto(
            entity.Id,
            entity.EntityDefinitionId,
            entity.Name,
            entity.DisplayName,
            entity.DataType,
            entity.IsRequired,
            entity.DefaultValue,
            entity.ValidationRulesJson,
            entity.Order,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
