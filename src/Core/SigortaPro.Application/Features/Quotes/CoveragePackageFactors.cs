using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

// Teminat paketi ölçek katsayıları. Prim ve teminat
// limitlerini paket seviyesine göre ölçekler. Risk skorunu etkilemez (paket, risk değil kapsam seçimidir).
internal static class CoveragePackageFactors
{
    public static decimal PremiumFactor(CoveragePackage package) => package switch
    {
        CoveragePackage.Standart => 1.00m,
        CoveragePackage.Genisletilmis => 1.30m,
        CoveragePackage.Premium => 1.60m,
        _ => 1.00m,
    };

    public static decimal CoverageLimitFactor(CoveragePackage package) => package switch
    {
        CoveragePackage.Standart => 1.00m,
        CoveragePackage.Genisletilmis => 1.50m,
        CoveragePackage.Premium => 2.00m,
        _ => 1.00m,
    };

    // Karşılaştırmada sunulacak paket seviyeleri.
    public static readonly IReadOnlyList<CoveragePackage> ComparablePackages = new[]
    {
        CoveragePackage.Standart,
        CoveragePackage.Genisletilmis,
        CoveragePackage.Premium,
    };
}
