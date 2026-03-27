namespace BuildingBlocks.Web.Models;

/// <summary>
/// Base for API response DTOs with common audit fields.
/// </summary>
public abstract record BaseResponse
{
    /// <summary>Entity identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>UTC timestamp when the entity was last updated (null if never updated).</summary>
    public DateTime? UpdatedAt { get; init; }
}
