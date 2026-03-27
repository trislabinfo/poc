using BuildingBlocks.Application.Auditing;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Database-backed security event logger (default implementation).
/// Logs security events; can be extended to persist to an audit table.
/// </summary>
internal sealed class DatabaseSecurityEventLogger : ISecurityEventLogger
{
    private readonly ILogger<DatabaseSecurityEventLogger> _logger;

    public DatabaseSecurityEventLogger(ILogger<DatabaseSecurityEventLogger> logger)
    {
        _logger = logger;
    }

    public Task LogLoginAttemptAsync(Guid userId, bool success, string ipAddress, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt: UserId={UserId}, Success={Success}, IP={IpAddress}", userId, success, ipAddress);
        return Task.CompletedTask;
    }

    public Task LogPermissionChangeAsync(Guid userId, string permission, string action, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Permission change: UserId={UserId}, Permission={Permission}, Action={Action}", userId, permission, action);
        return Task.CompletedTask;
    }

    public Task LogDataAccessAsync(Guid userId, string entityType, Guid entityId, string action, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Data access: UserId={UserId}, EntityType={EntityType}, EntityId={EntityId}, Action={Action}", userId, entityType, entityId, action);
        return Task.CompletedTask;
    }
}
