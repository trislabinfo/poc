using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Testing.Fakes;

namespace BuildingBlocks.Testing.Base;

/// <summary>
/// Base class for unit tests. Provides a shared <see cref="DateTimeProvider"/> (FakeDateTimeProvider)
/// and ensures FluentAssertions is available for assertions.
/// </summary>
public abstract class BaseUnitTest
{
    /// <summary>
    /// Shared fake date/time provider for deterministic tests. Override in derived class or set in constructor if needed.
    /// </summary>
    protected IDateTimeProvider DateTimeProvider { get; } = new FakeDateTimeProvider();

    /// <summary>
    /// Returns the fake as <see cref="FakeDateTimeProvider"/> when you need to advance time or set a specific value.
    /// </summary>
    protected FakeDateTimeProvider FakeDateTime => (FakeDateTimeProvider)DateTimeProvider;
}
