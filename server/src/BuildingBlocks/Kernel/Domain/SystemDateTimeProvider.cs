namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// System implementation of <see cref="IDateTimeProvider"/> that returns <see cref="DateTime.UtcNow"/>.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

