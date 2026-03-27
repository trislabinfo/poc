using BuildingBlocks.Application.RequestDispatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Capabilities.Messaging.InProcess.Behaviors;

internal sealed class PerformanceBehavior<TRequest, TResponse> : IRequestPipelineBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly IOptions<PerformanceBehaviorOptions> _options;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        IOptions<PerformanceBehaviorOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > _options.Value.ThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Slow request: {RequestName} took {ElapsedMs}ms (threshold {ThresholdMs}ms)",
                requestName, stopwatch.ElapsedMilliseconds, _options.Value.ThresholdMilliseconds);
        }
        return response;
    }
}
