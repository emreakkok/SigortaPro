using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Pricing;

// Yerleşik baz tarifeyi tek kaynaktan (PricingRuleTables) açar. Motor da fiyatı buradan okur;
// böylece admin ekranındaki "yerleşik varsayılan" gösterimi ile gerçek hesaplama BİRBİRİNDEN ASLA
// SAPMAZ (frontend'de fiyat kopyalanmaz — sahte veri riski yoktur). Stateless → Singleton.
internal sealed class PricingBaselineProvider : IPricingBaselineProvider
{
    public IReadOnlyDictionary<InsuranceBranch, decimal> BaselineBasePremiums => PricingRuleTables.BasePremiums;

    public IReadOnlyDictionary<string, decimal> BaselineCityRiskCoefficients => PricingRuleTables.CityRiskCoefficients;

    public decimal BaselineDefaultCityRiskCoefficient => PricingRuleTables.DefaultCityRiskCoefficient;

    public PricingBandBaselines BandBaselines => new(
        PricingRuleTables.DriverAgeBaseline,
        PricingRuleTables.VehicleAgeBaseline,
        PricingRuleTables.EnginePowerBaseline,
        PricingRuleTables.VehicleUsageBaseline,
        PricingRuleTables.BonusMalusBaseline,
        PricingRuleTables.BuildingAgeBaseline,
        PricingRuleTables.SquareMetersBaseline,
        PricingRuleTables.EarthquakeZoneBaseline,
        PricingRuleTables.HealthAgeBaseline,
        PricingRuleTables.SmokerSurchargeBaseline);
}
