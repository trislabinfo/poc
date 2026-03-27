namespace BuildingBlocks.Application.ErrorTracking;

/// <summary>
/// Abstraction for error tracking and monitoring.
/// Implementations: Sentry, Application Insights, Raygun, Rollbar.
/// </summary>
public interface IErrorTracker
{
    void CaptureException(Exception exception, ErrorContext context);

    void CaptureMessage(string message, ErrorSeverity severity, ErrorContext context);
}
