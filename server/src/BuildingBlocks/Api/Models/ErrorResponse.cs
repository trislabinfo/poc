namespace BuildingBlocks.Web.Models;

/// <summary>
/// Standard error response body for API failures.
/// </summary>
/// <param name="Code">Error code.</param>
/// <param name="Message">Human-readable message.</param>
/// <param name="CorrelationId">Request correlation ID when available.</param>
/// <param name="Timestamp">UTC timestamp of the response.</param>
public sealed record ErrorResponse(
    string Code,
    string Message,
    string? CorrelationId = null,
    DateTime? Timestamp = null);
