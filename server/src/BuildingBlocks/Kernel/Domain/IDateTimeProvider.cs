namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Provides access to the current time in UTC.
/// </summary>
/// <remarks>
/// Abstracting time improves testability and enables deterministic domain logic.
/// </remarks>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    DateTime UtcNow { get; }
}

