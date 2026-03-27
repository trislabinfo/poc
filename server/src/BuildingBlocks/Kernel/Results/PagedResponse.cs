namespace BuildingBlocks.Kernel.Results;

/// <summary>
/// Wrapper for paginated query results.
/// </summary>
/// <param name="Items">Page items.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="TotalCount">Total number of items.</param>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
