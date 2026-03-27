using System.Linq.Expressions;

namespace BuildingBlocks.Application.BackgroundJobs;

/// <summary>
/// Abstraction over a background job scheduler (Hangfire, Quartz.NET, etc.).
/// </summary>
public interface IBackgroundJobScheduler
{
    string Enqueue<T>(Expression<Action<T>> methodCall);

    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);

    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression);

    void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression);
}

