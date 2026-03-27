namespace BuildingBlocks.Web.Models;

/// <summary>
/// Base API response envelope for all API responses. Provides a consistent structure for success and error.
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error details when Success is false.
    /// </summary>
    public ErrorResponse? Error { get; set; }

    /// <summary>
    /// UTC timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful API response (no data).
    /// </summary>
    public static ApiResponse CreateSuccess()
    {
        return new ApiResponse { Success = true };
    }

    /// <summary>
    /// Creates a failed API response with error details.
    /// </summary>
    public static ApiResponse CreateFailure(ErrorResponse error)
    {
        return new ApiResponse
        {
            Success = false,
            Error = error
        };
    }
}
