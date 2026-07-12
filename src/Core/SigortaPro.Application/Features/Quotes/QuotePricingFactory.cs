using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

// Teklif modülünün fiyatlama köprüsü: domain verisinden Task 8 fiyatlama motorunun (saf/deterministik)
// girdisini kurar, teminat paketi ölçeğini uygular ve prim dökümü + ölçekli teminatları üretir.
// Aynı referans tarihiyle çağrıldığında aynı sonucu verir; bu sayede teklif detayı, saklanan seçim +
// CreatedAt referansıyla oluşturma anındaki fiyatı birebir yeniden üretir (ADR-021).
internal static class QuotePricingFactory
{
    // Task 9 kapsamında hasarsızlık basamağı ve sigara beyanı alınmaz; motor varsayılanlarıyla çalışılır
    // (ileride teklif sihirbazı/yenileme akışında beyan olarak eklenebilir).
    private const int DefaultNoClaimTier = 0;
    private const bool DefaultIsSmoker = false;

    // claimHistoryFactor: yenileme tekliflerinde hasar geçmişi ek prim çarpanı (varsayılan 1.00 = etkisiz).
    // CoveragePackage gibi teklifte saklı bir girdi olduğundan yeniden hesapta aynı sonucu verir (ADR-021/ADR-025).
    public static QuotePricingOutcome Compute(
        IPricingEngine pricingEngine,
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        IEnumerable<Coverage> coverages,
        CoveragePackage package,
        DateTime referenceDate,
        decimal claimHistoryFactor = 1.00m)
    {
        var request = BuildRequest(branch, customer, vehicle, property, referenceDate);
        var result = pricingEngine.CalculatePremium(request);

        var premiumFactor = CoveragePackageFactors.PremiumFactor(package);

        var breakdown = new List<PricingBreakdownItem>(result.Breakdown)
        {
            new("Teminat Paketi", premiumFactor, $"{PackageDisplay(package)} paketi kapsam çarpanı."),
        };

        // Hasar geçmişi çarpanı yalnızca etkiliyse (yenileme) prim dökümüne eklenir; 1.00 ise dökümü değiştirmez.
        if (claimHistoryFactor != 1.00m)
        {
            breakdown.Add(new PricingBreakdownItem(
                "Hasar Geçmişi Çarpanı", claimHistoryFactor,
                "Önceki dönem hasar geçmişine göre yenileme ek primi."));
        }

        var totalPremium = Round(result.TotalPremium * premiumFactor * claimHistoryFactor);

        var limitFactor = CoveragePackageFactors.CoverageLimitFactor(package);
        var scaledCoverages = coverages
            .Select(coverage => new QuoteCoverageDto(
                coverage.Name,
                coverage.Description,
                Round(coverage.DefaultLimit * limitFactor)))
            .ToList();

        return new QuotePricingOutcome(result.BasePremium, totalPremium, result.RiskScore, breakdown, scaledCoverages);
    }

    public static PricingRequest BuildRequest(
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        DateTime referenceDate) => branch switch
    {
        InsuranceBranch.Kasko or InsuranceBranch.Trafik => new VehiclePricingRequest(
            branch,
            AgeInYears(customer.BirthDate, referenceDate),
            VehicleAgeInYears(vehicle!.ManufactureYear, referenceDate),
            vehicle.EnginePowerHp,
            customer.Address.City,
            DefaultNoClaimTier),
        InsuranceBranch.Konut or InsuranceBranch.Dask => new PropertyPricingRequest(
            branch,
            property!.BuildingAge,
            property.SquareMeters,
            property.EarthquakeZone),
        InsuranceBranch.Saglik => new HealthPricingRequest(
            AgeInYears(customer.BirthDate, referenceDate),
            DefaultIsSmoker),
        _ => throw new ArgumentOutOfRangeException(nameof(branch), branch, "Bilinmeyen sigorta branşı."),
    };

    public static string PackageDisplay(CoveragePackage package) => package switch
    {
        CoveragePackage.Standart => "Standart",
        CoveragePackage.Genisletilmis => "Genişletilmiş",
        CoveragePackage.Premium => "Premium",
        _ => package.ToString(),
    };

    private static int AgeInYears(DateTime birthDate, DateTime referenceDate)
    {
        var age = referenceDate.Year - birthDate.Year;
        if (birthDate.Date > referenceDate.Date.AddYears(-age))
        {
            age--;
        }

        return Math.Max(0, age);
    }

    private static int VehicleAgeInYears(int manufactureYear, DateTime referenceDate) =>
        Math.Max(0, referenceDate.Year - manufactureYear);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

// Fiyatlama köprüsünün ürettiği, teklif DTO'suna eşlenecek ara sonuç.
internal sealed record QuotePricingOutcome(
    decimal BasePremium,
    decimal TotalPremium,
    RiskScore RiskScore,
    IReadOnlyList<PricingBreakdownItem> Breakdown,
    IReadOnlyList<QuoteCoverageDto> Coverages);
