using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SigortaPro.Persistence.Conversions;

// TIMEZONE: Sistemdeki tüm "instant" DateTime alanları UTC olarak saklanır (ADR — timezone stratejisi).
// EF Core, SQL Server `datetime2` (ve SQLite TEXT) kolonlarından bir DateTime materialize ederken
// Kind=Unspecified üretir; bu, System.Text.Json'ın çıktıya "Z" koymamasına ve istemcinin UTC değeri
// yerel saat sanmasına yol açar (13:23 → 10:23 kök nedeni). Bu converter, OKUMA anında değeri
// Kind=Utc olarak işaretler → serileştirme ISO-8601 + "Z" üretir. YAZMA tarafı kimliktir: değerler zaten
// UTC'dir (UtcNow / frontend toISOString) ve DB'deki bit'ler değiştirilmez (mevcut veri korunur).
//
// DİKKAT: Yalnızca "instant" alanlara uygulanır. Date-only alanlar (ör. BirthDate) bu dönüşümün DIŞINDA
// tutulur (bkz. AppDbContext) — takvim günü timezone'a tabi değildir; gün kayması riski önlenir.
internal static class UtcDateTimeConverters
{
    public static readonly ValueConverter<DateTime, DateTime> Utc =
        new(write => write, read => DateTime.SpecifyKind(read, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> NullableUtc =
        new(
            write => write,
            read => read.HasValue ? DateTime.SpecifyKind(read.Value, DateTimeKind.Utc) : read);
}
