using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Application;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for data source definition (AppBuilder and TenantApplication).</summary>
public static class DataSourceDefinitionMapper
{
    public static DataSourceDefinitionDto ToDto(DataSourceDefinition entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new DataSourceDefinitionDto(
            entity.Id,
            entity.AppDefinitionId,
            entity.Name,
            entity.Type,
            entity.ConfigurationJson,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
