using AppDefinition.Domain.Entities.Lifecycle;

namespace AppDefinition.Contracts.DTOs;

/// <summary>Shared response DTO for application release (AppBuilder and TenantApplication).</summary>
public sealed record ApplicationReleaseDto(
    Guid Id,
    Guid AppDefinitionId,
    string Version,
    int Major,
    int Minor,
    int Patch,
    string ReleaseNotes,
    DateTime ReleasedAt,
    Guid ReleasedBy,
    bool IsActive,
    DateTime CreatedAt,
    string? DdlScriptsJson = null,
    DdlScriptStatus? DdlScriptsStatus = null,
    DateTime? ApprovedAt = null,
    Guid? ApprovedBy = null);
