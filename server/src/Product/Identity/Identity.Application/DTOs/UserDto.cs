namespace Identity.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    Guid DefaultTenantId,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
