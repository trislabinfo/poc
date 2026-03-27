using TenantApplication.Domain.Enums;

namespace TenantApplication.Application.DTOs;

public sealed record TenantApplicationDto(
    Guid Id,
    Guid TenantId,
    Guid? ApplicationReleaseId,
    Guid? ApplicationId,
    int? Major,
    int? Minor,
    int? Patch,
    string Name,
    string Slug,
    string? Description,
    bool IsCustom,
    Guid? SourceApplicationReleaseId,
    TenantApplicationStatus Status,
    string ConfigurationJson,
    DateTime? InstalledAt,
    DateTime? ActivatedAt,
    DateTime CreatedAt);
