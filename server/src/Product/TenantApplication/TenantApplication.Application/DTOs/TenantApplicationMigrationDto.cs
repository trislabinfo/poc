using TenantApplication.Domain.Enums;

namespace TenantApplication.Application.DTOs;

public sealed record TenantApplicationMigrationDto(
    Guid Id,
    Guid TenantApplicationEnvironmentId,
    Guid? FromReleaseId,
    Guid ToReleaseId,
    string MigrationScriptJson,
    MigrationStatus Status,
    DateTime? ExecutedAt,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
