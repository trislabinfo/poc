using BuildingBlocks.Application.ErrorTracking;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.ErrorTracking;

/// <summary>
/// No-op implementation of <see cref="IErrorTracker"/>.
/// Logs errors but does NOT send them to an external service.
/// </summary>
internal sealed class NullErrorTracker : IErrorTracker
{
    private readonly ILogger<NullErrorTracker> _logger;

    public NullErrorTracker(ILogger<NullErrorTracker> logger)
    {
        _logger = logger;
    }

    public void CaptureException(Exception exception, ErrorContext context)
    {
        _logger.LogError(exception,
            "Error captured (NullErrorTracker): CorrelationId={CorrelationId}, TenantId={TenantId}, UserId={UserId}",
            context.CorrelationId, context.TenantId, context.UserId);
    }

    public void CaptureMessage(string message, ErrorSeverity severity, ErrorContext context)
    {
        _logger.LogWarning(
            "Message captured (NullErrorTracker): {Message}, Severity={Severity}, CorrelationId={CorrelationId}",
            message, severity, context.CorrelationId);
    }
}
