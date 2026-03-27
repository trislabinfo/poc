namespace BuildingBlocks.Application.Auditing;

/// <summary>
/// Abstraction for security event logging (login attempts, permission changes, data access).
/// </summary>
public interface ISecurityEventLogger
{
    Task LogLoginAttemptAsync(Guid userId, bool success, string ipAddress, CancellationToken cancellationToken = default);

    Task LogPermissionChangeAsync(Guid userId, string permission, string action, CancellationToken cancellationToken = default);

    Task LogDataAccessAsync(Guid userId, string entityType, Guid entityId, string action, CancellationToken cancellationToken = default);
}
