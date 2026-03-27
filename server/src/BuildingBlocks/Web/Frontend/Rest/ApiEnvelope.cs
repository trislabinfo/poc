namespace BuildingBlocks.Web.Rest;

/// <summary>
/// Shared envelope format used by backend APIs.
/// </summary>
public sealed class ApiEnvelope<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiErrorEnvelope? Error { get; set; }
}

public sealed class ApiErrorEnvelope
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}

