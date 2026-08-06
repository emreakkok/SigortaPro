using System.Reflection;
using System.Text.Json;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Vehicles.DTOs;

namespace SigortaPro.Infrastructure.Services.VehicleCatalog;

// Araç kataloğu, harici API veya yeni tablo yerine Infrastructure'da gömülü JSON kaynağından okunur.
// Veri bir defa (thread-safe, Lazy) yüklenip In-Memory cache'lenir → sonraki çağrılar diske/kaynağa gitmez.
// DI: Singleton. Migration gerektirmez, API bağımsızlığı sağlar.
public sealed class VehicleCatalogProvider : IVehicleCatalogProvider
{
    private const string ResourceFileName = "vehicle-catalog.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Lazy<VehicleCatalogDto> _catalog =
        new(LoadCatalog, LazyThreadSafetyMode.ExecutionAndPublication);

    public VehicleCatalogDto GetCatalog() => _catalog.Value;

    private static VehicleCatalogDto LoadCatalog()
    {
        var assembly = typeof(VehicleCatalogProvider).Assembly;

        // Kaynak adı, ad alanı yeniden yapılandırılırsa kırılmasın diye dosya adına göre bulunur.
        var resourceName = Array.Find(
                assembly.GetManifestResourceNames(),
                name => name.EndsWith(ResourceFileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Gömülü araç kataloğu kaynağı bulunamadı: {ResourceFileName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Araç kataloğu kaynağı açılamadı: {resourceName}");
        using var reader = new StreamReader(stream);

        var json = reader.ReadToEnd();

        return JsonSerializer.Deserialize<VehicleCatalogDto>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Araç kataloğu ayrıştırılamadı (boş içerik).");
    }
}
