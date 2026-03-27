namespace BuildingBlocks.Kernel.Results;

/// <summary>
/// Result type representing success or failure with a value.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Failure details when <see cref="IsFailure"/> is true.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Successful value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failure result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value when the result is failure.");

    public static Result<T> Success(T value) => new(true, value, Error.None);

    public static Result<T> Failure(Error error) => new(false, default, error);

    public static Result<T> Failure(string message) => new(false, default, Error.Failure(message));
}

