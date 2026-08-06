using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

// Teklif modülünün fiyatlama köprüsü: fiyatlama motorunun (saf/deterministik) girdisini kurar, teminat
// paketi ölçeğini uygular ve prim dökümü + ölçekli teminatları üretir.
//
// Girdiler artık teklifte SAKLANIR (PricingSnapshot). Yeniden hesap öncelikle snapshot'tan okur;
// snapshot yoksa (bu alan eklenmeden önce oluşmuş kayıtlar) eski davranışa — canlı entity'den okumaya —
// düşer. Böylece müşteri adresini/aracını sonradan değiştirse bile eski teklifin risk skoru ve prim
// dökümü DEĞİŞMEZ, mevcut kayıtlar da bit-aynı kalır.
internal static class QuotePricingFactory
{
    // Beyanı alınmamış (eski) sağlık tekliflerinde motor girdisi: sigara faktörü etkisiz kalır ve
    // dökümde gösterilmez — kullanıcıya sorulmamış bir sağlık beyanı gerçekmiş gibi sunulmaz.
    private const bool NoSmokerDeclaration = false;

    // Gerçek veriye dayanmadığı sürece kullanıcıya gösterilmeyecek döküm kalemi (motor adıyla birebir).
    private const string SmokerFactorName = "Sigara Kullanımı";

    /// <summary>
    /// Teklif oluşturulurken fiyatlama girdilerini dondurur. Yalnızca fiyatı DOĞRUDAN belirleyen
    /// primitifler kopyalanır; kişisel veri (TCKN, telefon, tam adres) ve fiyata etkisiz alanlar (plaka,
    /// marka, model) taşınmaz.
    /// </summary>
    public static PricingSnapshot BuildSnapshot(
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        DateTime referenceDate,
        DateTime? insuredBirthDate = null,
        bool? isSmoker = null,
        int? earthquakeZone = null,
        int bonusMalusStep = 0) => branch switch
    {
        InsuranceBranch.Kasko or InsuranceBranch.Trafik => PricingSnapshot.ForVehicle(
            referenceDate,
            AgeInYears(customer.BirthDate, referenceDate),
            VehicleAgeInYears(vehicle!.ManufactureYear, referenceDate),
            vehicle.EnginePowerHp,
            // MVP: müşterinin adres ili, aracın kullanım ili için vekildir (teknik borç).
            customer.Address.City,
            bonusMalusStep,
            // Kullanım amacı beyanı teklif anında dondurulur → araç sonradan güncellense bile
            // bu teklifin fiyatı ve dökümü değişmez.
            vehicle.UsagePurpose),
        InsuranceBranch.Konut or InsuranceBranch.Dask => PricingSnapshot.ForProperty(
            referenceDate,
            property!.BuildingAge,
            property.SquareMeters,
            earthquakeZone ?? EarthquakeZoneDefaults.Unknown),
        InsuranceBranch.Saglik => PricingSnapshot.ForHealth(
            referenceDate,
            AgeInYears(insuredBirthDate ?? customer.BirthDate, referenceDate),
            isSmoker),
        _ => throw new ArgumentOutOfRangeException(nameof(branch), branch, "Bilinmeyen sigorta branşı."),
    };

    // claimHistoryFactor: yenileme tekliflerinde hasar geçmişi ek prim çarpanı (varsayılan 1.00 = etkisiz).
    // rates: teklifin sabitlediği tarife versiyonunun baz primleri.
    // snapshot: teklifin dondurulmuş girdileri; null ise canlı veriden hesaplanır (eski kayıtlar).
    // renewalDiscountFactor: yenileme tekliflerinde aktif tarifenin yenileme indirimi
    // (1.00 = indirim yok). Teklifte saklanır → deterministik yeniden hesapta aynı değer verilir.
    public static QuotePricingOutcome Compute(
        IPricingEngine pricingEngine,
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        IEnumerable<Coverage> coverages,
        CoveragePackage package,
        DateTime referenceDate,
        decimal claimHistoryFactor = 1.00m,
        DateTime? insuredBirthDate = null,
        PricingRateSet? rates = null,
        PricingSnapshot? snapshot = null,
        decimal renewalDiscountFactor = 1.00m)
    {
        var request = snapshot is null
            ? BuildRequest(branch, customer, vehicle, property, referenceDate, insuredBirthDate)
            : BuildRequestFromSnapshot(branch, snapshot);

        var result = pricingEngine.CalculatePremium(request, rates);

        // Paket (kapsam) prim çarpanı versiyonlanmıştır: tarifenin kural seti varsa ORADAN, yoksa yerleşik baseline.
        var premiumFactor = rates?.RuleSet?.PackagePremiumFactorFor(package)
            ?? CoveragePackageFactors.PremiumFactor(package);

        // Gerçek veriye dayanmayan kalemler dökümden çıkarılır. Tutar motorun kendi hesabından geldiği için
        // (aşağıdaki totalPremium) bu filtre HİÇBİR fiyatı değiştirmez — yalnızca sunumu dürüstleştirir.
        var breakdown = result.Breakdown
            .Where(item => IsBackedByRealData(item, snapshot))
            .ToList();

        breakdown.Add(new PricingBreakdownItem(
            "Teminat Paketi", premiumFactor, $"{PackageDisplay(package)} paketi kapsam çarpanı."));

        // Hasar geçmişi çarpanı yalnızca etkiliyse (yenileme) prim dökümüne eklenir; 1.00 ise dökümü değiştirmez.
        if (claimHistoryFactor != 1.00m)
        {
            breakdown.Add(new PricingBreakdownItem(
                "Hasar Geçmişi Çarpanı", claimHistoryFactor,
                "Önceki dönem hasar geçmişine göre yenileme ek primi."));
        }

        // Yenileme indirimi yalnızca etkiliyse (yenileme + aktif tarifede indirim tanımlı) dökümde görünür.
        if (renewalDiscountFactor != 1.00m)
        {
            breakdown.Add(new PricingBreakdownItem(
                "Yenileme İndirimi", renewalDiscountFactor,
                "Yenileme dönemine özel indirim (aktif tarife)."));
        }

        var totalPremium = Round(result.TotalPremium * premiumFactor * claimHistoryFactor * renewalDiscountFactor);

        var limitFactor = CoveragePackageFactors.CoverageLimitFactor(package);
        var scaledCoverages = coverages
            .Select(coverage => new QuoteCoverageDto(
                coverage.Name,
                coverage.Description,
                Round(coverage.DefaultLimit * limitFactor)))
            .ToList();

        return new QuotePricingOutcome(result.BasePremium, totalPremium, result.RiskScore, breakdown, scaledCoverages);
    }

    // Kullanıcıya yalnızca gerçek veriye dayanan faktörler gösterilir.
    // Hasarsızlık basamağı itibaren gerçek veriden türetildiğinden artık burada filtrelenmez;
    // motor zaten yalnızca basamak nötr DEĞİLSE kalem üretir (nötr/eski kayıtlarda döküm değişmez).
    private static bool IsBackedByRealData(PricingBreakdownItem item, PricingSnapshot? snapshot)
    {
        if (item.Factor == SmokerFactorName)
        {
            return snapshot?.IsSmoker is not null;
        }

        return true;
    }

    // Snapshot'tan motor girdisi — canlı entity'ye HİÇ dokunulmaz.
    private static PricingRequest BuildRequestFromSnapshot(InsuranceBranch branch, PricingSnapshot snapshot) =>
        branch switch
        {
            InsuranceBranch.Kasko or InsuranceBranch.Trafik => new VehiclePricingRequest(
                branch,
                snapshot.DriverAge ?? 0,
                snapshot.VehicleAge ?? 0,
                snapshot.EnginePowerHp ?? 0,
                snapshot.RiskCity ?? string.Empty,
                snapshot.NoClaimTier ?? 0,
                snapshot.UsagePurpose),
            InsuranceBranch.Konut or InsuranceBranch.Dask => new PropertyPricingRequest(
                branch,
                snapshot.BuildingAge ?? 0,
                snapshot.SquareMeters ?? 0,
                snapshot.EarthquakeZone ?? EarthquakeZoneDefaults.Unknown),
            InsuranceBranch.Saglik => new HealthPricingRequest(
                snapshot.InsuredAge ?? 0,
                snapshot.IsSmoker ?? NoSmokerDeclaration),
            _ => throw new ArgumentOutOfRangeException(nameof(branch), branch, "Bilinmeyen sigorta branşı."),
        };

    // Eski (snapshot'sız) kayıtlar ve teklif oluşmadan yapılan önizleme (karşılaştırma) için canlı veriden girdi.
    public static PricingRequest BuildRequest(
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        DateTime referenceDate,
        DateTime? insuredBirthDate = null) => branch switch
    {
        InsuranceBranch.Kasko or InsuranceBranch.Trafik => new VehiclePricingRequest(
            branch,
            AgeInYears(customer.BirthDate, referenceDate),
            VehicleAgeInYears(vehicle!.ManufactureYear, referenceDate),
            vehicle.EnginePowerHp,
            customer.Address.City,
            // Snapshot'sız (eski) kayıtlarda Bonus-Malus basamağı BİLİNÇLİ olarak 0'dır —
            // müşterinin bugünkü hasar geçmişini geçmiş teklife yansıtmak, o teklifin primini ve dökümünü
            // geriye dönük değiştirirdi.
            BonusMalusScale.NeutralStep,
            // Snapshot'sız kayıtlarda kullanım amacı da uygulanmaz (aynı gerekçe).
            UsagePurpose: null),
        InsuranceBranch.Konut or InsuranceBranch.Dask => new PropertyPricingRequest(
            branch,
            property!.BuildingAge,
            property.SquareMeters,
            property.EarthquakeZone),
        // Sağlıkta yaş, "başkası adına" teklifte sigortalının doğum tarihinden hesaplanır.
        InsuranceBranch.Saglik => new HealthPricingRequest(
            AgeInYears(insuredBirthDate ?? customer.BirthDate, referenceDate),
            NoSmokerDeclaration),
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
