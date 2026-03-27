namespace BuildingBlocks.Application.Logging;

/// <summary>
/// Abstraction for structured logging.
/// Implementations: Serilog, NLog, Microsoft.Extensions.Logging.
/// </summary>
public interface IStructuredLogger
{
    void LogInformation(string messageTemplate, params object[] propertyValues);

    void LogWarning(string messageTemplate, params object[] propertyValues);

    void LogError(Exception exception, string messageTemplate, params object[] propertyValues);

    void LogDebug(string messageTemplate, params object[] propertyValues);
}
