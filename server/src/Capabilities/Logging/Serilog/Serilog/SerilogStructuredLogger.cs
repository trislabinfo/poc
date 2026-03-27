using BuildingBlocks.Application.Logging;
using Serilog;

namespace Capabilities.Logging.Serilog;

internal sealed class SerilogStructuredLogger : IStructuredLogger
{
    private readonly ILogger _logger;

    public SerilogStructuredLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInformation(string messageTemplate, params object[] propertyValues) =>
        _logger.Information(messageTemplate, propertyValues);

    public void LogWarning(string messageTemplate, params object[] propertyValues) =>
        _logger.Warning(messageTemplate, propertyValues);

    public void LogError(Exception exception, string messageTemplate, params object[] propertyValues) =>
        _logger.Error(exception, messageTemplate, propertyValues);

    public void LogDebug(string messageTemplate, params object[] propertyValues) =>
        _logger.Debug(messageTemplate, propertyValues);
}
