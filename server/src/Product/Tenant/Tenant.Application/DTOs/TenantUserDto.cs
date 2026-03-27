namespace Tenant.Application.DTOs;

public sealed record TenantUserDto(Guid Id, Guid UserId, bool IsTenantOwner);
