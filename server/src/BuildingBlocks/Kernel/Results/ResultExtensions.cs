namespace BuildingBlocks.Kernel.Results;

/// <summary>
/// Extension methods for Result and Result{T} to enable functional composition.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a Result{T} to Result{TNew} using a transformation function.
    /// Only executes the mapper if the result is successful.
    /// </summary>
    /// <typeparam name="T">Source result type.</typeparam>
    /// <typeparam name="TNew">Target result type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="mapper">Function to transform T to TNew.</param>
    /// <returns>Result{TNew} with mapped value or original error.</returns>
    public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
    {
        return result.IsSuccess
            ? Result<TNew>.Success(mapper(result.Value))
            : Result<TNew>.Failure(result.Error);
    }

    /// <summary>
    /// Binds a Result{T} to Result{TNew} using a function that returns Result{TNew}.
    /// Only executes the binder if the result is successful.
    /// </summary>
    /// <typeparam name="T">Source result type.</typeparam>
    /// <typeparam name="TNew">Target result type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="binder">Function to transform T to Result{TNew}.</param>
    /// <returns>Result{TNew} from binder or original error.</returns>
    public static Result<TNew> Bind<T, TNew>(this Result<T> result, Func<T, Result<TNew>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : Result<TNew>.Failure(result.Error);
    }

    /// <summary>
    /// Executes an action if the result is successful. Returns the original result.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">Action to execute on success.</param>
    /// <returns>The original result.</returns>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Combines multiple Result objects. Returns failure if any result failed.
    /// Returns the first failure encountered.
    /// </summary>
    /// <param name="results">Results to combine.</param>
    /// <returns>Success if all results succeeded; first failure otherwise.</returns>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    /// <summary>
    /// Converts Result{T} to Result by discarding the value.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>Result without value.</returns>
    public static Result ToResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error);
    }
}
