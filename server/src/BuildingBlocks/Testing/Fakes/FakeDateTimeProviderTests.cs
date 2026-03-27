using FluentAssertions;
using Xunit;

namespace BuildingBlocks.Testing.Fakes;

public sealed class FakeDateTimeProviderTests
{
    [Fact]
    public void Constructor_WithDefault_ReturnsFixedUtcTime()
    {
        var sut = new FakeDateTimeProvider();
        sut.UtcNow.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        sut.UtcNow.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_WithValue_ReturnsThatUtcTime()
    {
        var value = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
        var sut = new FakeDateTimeProvider(value);
        sut.UtcNow.Should().Be(value);
    }

    [Fact]
    public void SetUtcNow_UpdatesReturnedTime()
    {
        var sut = new FakeDateTimeProvider();
        var newTime = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        sut.SetUtcNow(newTime);
        sut.UtcNow.Should().Be(newTime);
    }

    [Fact]
    public void Advance_AddsDuration()
    {
        var sut = new FakeDateTimeProvider(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        sut.Advance(TimeSpan.FromDays(1));
        sut.UtcNow.Should().Be(new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc));
    }
}
