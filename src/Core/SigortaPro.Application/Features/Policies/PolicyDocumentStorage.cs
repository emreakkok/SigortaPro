using System.Globalization;

namespace SigortaPro.Application.Features.Policies;

// Poliçe belgesi saklama anahtarları ve indirme dosya adı kuralları. Anahtar göreli tutulur;
// yerel disk veya blob implementasyonu aynı anahtarı kullanır.
internal static class PolicyDocumentStorage
{
    public const string PdfContentType = "application/pdf";

    // Depolama anahtarı: kararlı ve çakışmasız olması için poliçe Id'si kullanılır.
    public static string KeyFor(Guid policyId) => string.Create(
        CultureInfo.InvariantCulture, $"policy-documents/{policyId}.pdf");

    // İndirme dosya adı: kullanıcıya anlamlı poliçe numarası (örn. POL-2026-000002.pdf).
    public static string FileNameFor(string policyNumber) => $"{policyNumber}.pdf";
}
