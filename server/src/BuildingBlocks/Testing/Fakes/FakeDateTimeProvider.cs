using BuildingBlocks.Kernel.Domain;

namespace BuildingBlocks.Testing.Fakes;

/// <summary>
/// Test double for <see cref="IDateTimeProvider"/> that returns a fixed or configurable UTC time.
/// </summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _utcNow;

    /// <summary>
    /// Creates a provider that returns the given fixed UTC time.
    /// </summary>
    public FakeDateTimeProvider(DateTime utcNow)
    {
        _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    /// <summary>
    /// Creates a provider that returns a default fixed time (e.g. 2025-01-01 00:00:00 UTC).
    /// </summary>
    public FakeDateTimeProvider()
        : this(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
    {
    }

    /// <inheritdoc />
    public DateTime UtcNow => _utcNow;

    /// <summary>
    /// Sets the time that will be returned by <see cref="UtcNow"/> (for tests that need to advance time).
    /// </summary>
    public void SetUtcNow(DateTime value)
    {
        _utcNow = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    /// <summary>
    /// Advances the returned time by the given duration.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _utcNow += duration;
    }
}
