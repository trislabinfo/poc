using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using FluentValidation;

namespace Capabilities.Messaging.InProcess.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse> : IRequestPipelineBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var message = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
        var error = Error.Validation($"{typeof(TRequest).Name}.Validation", message);
        return ResultResponseFactory.CreateFailure<TResponse>(error);
    }
}

internal static class ResultResponseFactory
{
    public static TResponse CreateFailure<TResponse>(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod(
                nameof(Result.Failure),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                [typeof(Error)]);
            if (failureMethod is null)
                failureMethod = typeof(TResponse).GetMethod(
                    "Failure",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    [typeof(Error)]);
            if (failureMethod is not null)
                return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior can only return Result or Result<T> responses. Actual: {typeof(TResponse).FullName}");
    }
}
