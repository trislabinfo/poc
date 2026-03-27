using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for relation definition (AppBuilder and TenantApplication).</summary>
public static class RelationDefinitionMapper
{
    public static RelationDefinitionDto ToDto(RelationDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new RelationDefinitionDto(
            entity.Id,
            entity.SourceEntityId,
            entity.TargetEntityId,
            entity.Name,
            entity.RelationType,
            entity.CascadeDelete,
            entity.CreatedAt);
    }
}
