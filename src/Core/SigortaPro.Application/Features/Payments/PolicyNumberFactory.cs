using System.Globalization;
using SigortaPro.Domain.Constants;

namespace SigortaPro.Application.Features.Payments;

// Sıralı poliçe numarası biçimlendirir: POL-{yıl}-{6 haneli sıra}. Örn: POL-2026-000001.
// Sıra numarası, ilgili yıla ait mevcut poliçe sayısından türetilir; benzersizliği DB unique index garanti eder.
internal static class PolicyNumberFactory
{
    public static string Format(int year, int sequence) => string.Create(
        CultureInfo.InvariantCulture,
        $"{BusinessConstants.PolicyNumberPrefix}-{year}-{sequence:D6}");
}
