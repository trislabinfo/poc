using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Value converter that ensures DateTime values are UTC when saving to PostgreSQL timestamp with time zone columns.
/// Use this converter for all DateTime properties to prevent "Cannot write DateTime with Kind=Unspecified" errors.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

/// <summary>
/// Value converter that ensures nullable DateTime values are UTC when saving to PostgreSQL timestamp with time zone columns.
/// Use this converter for all nullable DateTime properties to prevent "Cannot write DateTime with Kind=Unspecified" errors.
/// </summary>
public class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter()
        : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : null,
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : null)
    {
    }
}
