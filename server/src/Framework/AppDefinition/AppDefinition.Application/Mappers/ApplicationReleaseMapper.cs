using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Entities.Lifecycle;

namespace AppDefinition.Application.Mappers;

/// <summary>Shared mapper for application release (AppBuilder and TenantApplication).</summary>
public static class ApplicationReleaseMapper
{
    public static ApplicationReleaseDto ToDto(ApplicationRelease entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new ApplicationReleaseDto(
            entity.Id,
            entity.AppDefinitionId,
            entity.Version,
            entity.Major,
            entity.Minor,
            entity.Patch,
            entity.ReleaseNotes,
            entity.ReleasedAt,
            entity.ReleasedBy,
            entity.IsActive,
            entity.CreatedAt,
            entity.DdlScriptsJson,
            entity.DdlScriptsStatus,
            entity.ApprovedAt,
            entity.ApprovedBy);
    }

    /// <summary>Maps release to runtime snapshot DTO (navigation, pages, data sources, entities).</summary>
    public static ApplicationSnapshotDto ToSnapshotDto(ApplicationRelease entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new ApplicationSnapshotDto(
            entity.NavigationJson,
            entity.PageJson,
            entity.DataSourceJson,
            entity.EntityJson,
            SchemaVersion: "1.0");
    }
}
