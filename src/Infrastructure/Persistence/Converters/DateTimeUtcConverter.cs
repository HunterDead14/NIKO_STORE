using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

public sealed class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter()
        : base(
            value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
