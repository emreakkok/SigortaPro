using System.Reflection;
using System.Text.Json;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Cities.DTOs;

namespace SigortaPro.Infrastructure.Services.CityCatalog;

// İl kataloğu, harici API veya yeni tablo yerine Infrastructure'da gömülü JSON kaynağından okunur.
// Veri bir defa (thread-safe, Lazy) yüklenip In-Memory cache'lenir → sonraki çağrılar diske/kaynağa gitmez.
// DI: Singleton. VehicleCatalogProvider deseninin birebir izidir.
public sealed class CityCatalogProvider : ICityCatalogProvider
{
    private const string ResourceFileName = "city-catalog.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Lazy<CityCatalogDto> _catalog =
        new(LoadCatalog, LazyThreadSafetyMode.ExecutionAndPublication);

    public CityCatalogDto GetCatalog() => _catalog.Value;

    private static CityCatalogDto LoadCatalog()
    {
        var assembly = typeof(CityCatalogProvider).Assembly;

        // Kaynak adı, ad alanı yeniden yapılandırılırsa kırılmasın diye dosya adına göre bulunur.
        var resourceName = Array.Find(
                assembly.GetManifestResourceNames(),
                name => name.EndsWith(ResourceFileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Gömülü il kataloğu kaynağı bulunamadı: {ResourceFileName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"İl kataloğu kaynağı açılamadı: {resourceName}");
        using var reader = new StreamReader(stream);

        var json = reader.ReadToEnd();

        return JsonSerializer.Deserialize<CityCatalogDto>(json, SerializerOptions)
            ?? throw new InvalidOperationException("İl kataloğu ayrıştırılamadı (boş içerik).");
    }
}
