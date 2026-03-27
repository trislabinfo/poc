using BuildingBlocks.Application.ErrorTracking;

namespace Capabilities.ErrorTracking.Sentry;

internal sealed class SentryErrorTracker : IErrorTracker
{
    public void CaptureException(Exception exception, ErrorContext context)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            if (context.CorrelationId != null)
                scope.SetTag("correlation_id", context.CorrelationId);

            if (context.TenantId.HasValue)
                scope.SetTag("tenant_id", context.TenantId.Value.ToString());

            if (context.UserId.HasValue)
                scope.User = new SentryUser { Id = context.UserId.Value.ToString() };

            if (context.RequestPath != null)
                scope.SetTag("request_path", context.RequestPath);

            if (context.AdditionalData != null)
            {
                foreach (var (key, value) in context.AdditionalData)
                {
                    scope.SetExtra(key, value);
                }
            }
        });
    }

    public void CaptureMessage(string message, ErrorSeverity severity, ErrorContext context)
    {
        var sentryLevel = severity switch
        {
            ErrorSeverity.Debug => SentryLevel.Debug,
            ErrorSeverity.Info => SentryLevel.Info,
            ErrorSeverity.Warning => SentryLevel.Warning,
            ErrorSeverity.Error => SentryLevel.Error,
            ErrorSeverity.Fatal => SentryLevel.Fatal,
            _ => SentryLevel.Info
        };

        SentrySdk.CaptureMessage(message, sentryLevel);
    }
}
