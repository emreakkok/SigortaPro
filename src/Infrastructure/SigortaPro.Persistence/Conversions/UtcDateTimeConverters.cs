using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SigortaPro.Persistence.Conversions;

internal static class UtcDateTimeConverters
{
    public static readonly ValueConverter<DateTime, DateTime> Utc =
        new(write => write, read => DateTime.SpecifyKind(read, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> NullableUtc =
        new(
            write => write,
            read => read.HasValue ? DateTime.SpecifyKind(read.Value, DateTimeKind.Utc) : read);
}
