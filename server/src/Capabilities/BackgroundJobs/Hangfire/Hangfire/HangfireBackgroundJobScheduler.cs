using BuildingBlocks.Application.BackgroundJobs;
using Hangfire;
using System.Linq.Expressions;

namespace Capabilities.BackgroundJobs.Hangfire;

internal sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    public string Enqueue<T>(Expression<Action<T>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        BackgroundJob.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Action<T>> methodCall, string cronExpression) =>
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression) =>
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
}
