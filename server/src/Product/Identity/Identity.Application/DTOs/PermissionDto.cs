namespace Identity.Application.DTOs;

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Module,
    DateTime CreatedAt);
