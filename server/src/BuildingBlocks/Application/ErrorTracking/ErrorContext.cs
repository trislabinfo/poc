namespace BuildingBlocks.Application.ErrorTracking;

/// <summary>
/// Context passed when capturing errors for correlation and diagnostics.
/// </summary>
public sealed class ErrorContext
{
    public string? CorrelationId { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? UserId { get; init; }
    public string? RequestPath { get; init; }
    public Dictionary<string, object>? AdditionalData { get; init; }
}
