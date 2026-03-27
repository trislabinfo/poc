using TenantApplication.Domain.Enums;

namespace TenantApplication.Application.DTOs;

public sealed record TenantApplicationEnvironmentDto(
    Guid Id,
    Guid TenantApplicationId,
    string Name,
    EnvironmentType EnvironmentType,
    Guid? ApplicationReleaseId,
    string? ReleaseVersion,
    bool IsActive,
    DateTime? DeployedAt,
    DateTime CreatedAt);
