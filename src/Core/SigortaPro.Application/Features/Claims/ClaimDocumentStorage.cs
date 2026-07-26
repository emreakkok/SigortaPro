using System.Globalization;

namespace SigortaPro.Application.Features.Claims;

// Hasar belgesi depolama anahtarı kuralı (ADR-023 — IFileStorageService ile ortak). Anahtar göreli tutulur;
// yerel disk veya blob implementasyonu aynı anahtarı kullanır. Hasar bazında klasörlenir.
internal static class ClaimDocumentStorage
{
    // İzin verilen içerik türleri (hasar delili: foto veya PDF belge).
    public static readonly IReadOnlyCollection<string> AllowedContentTypes = new[]
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
    };

    public const int MaxDocumentCount = 5;
    public const long MaxDocumentSizeBytes = 3 * 1024 * 1024; // 3 MB / dosya

    public static string KeyFor(Guid claimId, Guid documentId) => string.Create(
        CultureInfo.InvariantCulture, $"claim-documents/{claimId}/{documentId}");

    public static bool IsImage(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
