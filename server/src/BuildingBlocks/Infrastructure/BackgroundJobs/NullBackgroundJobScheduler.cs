using BuildingBlocks.Application.BackgroundJobs;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BuildingBlocks.Infrastructure.BackgroundJobs;

/// <summary>
/// No-op implementation of <see cref="IBackgroundJobScheduler"/>.
/// Logs job scheduling requests but does NOT execute them.
/// </summary>
internal sealed class NullBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ILogger<NullBackgroundJobScheduler> _logger;

    public NullBackgroundJobScheduler(ILogger<NullBackgroundJobScheduler> logger)
    {
        _logger = logger;
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        _logger.LogWarning("Background job enqueued but NOT executed (NullBackgroundJobScheduler): {MethodCall}", methodCall);
        return Guid.NewGuid().ToString();
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        _logger.LogWarning("Background job enqueued but NOT executed (NullBackgroundJobScheduler): {MethodCall}", methodCall);
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        _logger.LogWarning("Background job scheduled but NOT executed (NullBackgroundJobScheduler): {MethodCall}, Delay: {Delay}", methodCall, delay);
        return Guid.NewGuid().ToString();
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        _logger.LogWarning("Background job scheduled but NOT executed (NullBackgroundJobScheduler): {MethodCall}, Delay: {Delay}", methodCall, delay);
        return Guid.NewGuid().ToString();
    }

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression)
    {
        _logger.LogWarning("Recurring job registered but NOT executed (NullBackgroundJobScheduler): {JobId}, {MethodCall}, Cron: {Cron}", recurringJobId, methodCall, cronExpression);
    }

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        _logger.LogWarning("Recurring job registered but NOT executed (NullBackgroundJobScheduler): {JobId}, {MethodCall}, Cron: {Cron}", recurringJobId, methodCall, cronExpression);
    }
}
