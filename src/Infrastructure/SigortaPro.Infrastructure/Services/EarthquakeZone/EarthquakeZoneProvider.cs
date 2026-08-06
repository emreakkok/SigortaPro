using System.Globalization;
using System.Reflection;
using System.Text.Json;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Infrastructure.Services.EarthquakeZone;

// Deprem bölgesi, konutun İLİNDEN türetilir (önceden kullanıcı serbestçe seçiyordu).
// Veri, harici API veya yeni tablo yerine gömülü JSON'dan bir defa yüklenip cache'lenir —
// CityCatalogProvider / VehicleCatalogProvider deseninin birebir izidir.
// DI: Singleton.
public sealed class EarthquakeZoneProvider : IEarthquakeZoneProvider
{
    private const string ResourceFileName = "earthquake-zone-catalog.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // İl adı → bölge. Eşleşme TÜRKÇE kültüre ve büyük/küçük harfe duyarsızdır: ordinal karşılaştırma
    // "istanbul" ile "İstanbul"u eşleştiremez (noktalı/noktasız I sorunu), bu da ilin tanınmamasına yol açardı.
    private readonly Lazy<IReadOnlyDictionary<string, int>> _zonesByCity =
        new(LoadZones, LazyThreadSafetyMode.ExecutionAndPublication);

    public int? ResolveZone(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        return _zonesByCity.Value.TryGetValue(city.Trim(), out var zone) ? zone : null;
    }

    private static Dictionary<string, int> LoadZones()
    {
        var assembly = typeof(EarthquakeZoneProvider).Assembly;

        var resourceName = Array.Find(
                assembly.GetManifestResourceNames(),
                name => name.EndsWith(ResourceFileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Gömülü deprem bölgesi kaynağı bulunamadı: {ResourceFileName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Deprem bölgesi kaynağı açılamadı: {resourceName}");
        using var reader = new StreamReader(stream);

        var catalog = JsonSerializer.Deserialize<EarthquakeZoneCatalog>(reader.ReadToEnd(), SerializerOptions)
            ?? throw new InvalidOperationException("Deprem bölgesi kataloğu ayrıştırılamadı (boş içerik).");

        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        var map = new Dictionary<string, int>(StringComparer.Create(turkish, ignoreCase: true));
        foreach (var entry in catalog.Zones)
        {
            foreach (var city in entry.Cities)
            {
                map[city] = entry.Zone;
            }
        }

        return map;
    }

    private sealed record EarthquakeZoneCatalog(IReadOnlyList<EarthquakeZoneEntry> Zones);

    private sealed record EarthquakeZoneEntry(int Zone, IReadOnlyList<string> Cities);
}
