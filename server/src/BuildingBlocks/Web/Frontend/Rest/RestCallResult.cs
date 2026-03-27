namespace BuildingBlocks.Web.Rest;

/// <summary>
/// Standard result wrapper for frontend REST calls.
/// </summary>
public sealed class RestCallResult<T>
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static RestCallResult<T> Success(T? data, int statusCode) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static RestCallResult<T> Failure(string message, int statusCode) =>
        new() { IsSuccess = false, ErrorMessage = message, StatusCode = statusCode };
}

