using AppBuilder.Domain.Enums;

namespace AppBuilder.Application.DTOs;

public sealed record AppDefinitionDto(
    Guid Id,
    string Name,
    string Description,
    string Slug,
    ApplicationStatus Status,
    string? CurrentVersion,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
