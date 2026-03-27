namespace BuildingBlocks.Web.Models;

/// <summary>
/// API response envelope for successful responses that return data. For paged lists use <see cref="ApiResponse{T}"/> with T = <see cref="BuildingBlocks.Kernel.Results.PagedResponse{TItem}"/>.
/// </summary>
/// <typeparam name="T">The type of data returned.</typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// The response data. Present when Success is true.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Creates a successful API response with data.
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed API response with error details.
    /// </summary>
    public new static ApiResponse<T> CreateFailure(ErrorResponse error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
}
