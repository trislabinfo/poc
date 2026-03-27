using AppBuilder.Application.DTOs;

namespace AppBuilder.Application.Mappers;

public static class AppDefinitionMapper
{
    public static AppDefinitionDto ToDto(AppDefinition.Domain.Entities.Application.AppDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new AppDefinitionDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.Slug,
            entity.Status,
            entity.CurrentVersion,
            entity.IsPublic,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
