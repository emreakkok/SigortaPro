using FluentAssertions;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;
using SigortaPro.Infrastructure.Services.Pricing;

namespace SigortaPro.Infrastructure.Tests.Services.Pricing;

public class PricingEngineTests
{
    private readonly PricingEngine _engine = new();

    // Tüm çarpanları 1.00 olan (standart) araç girdisi; branşa göre baz prim aynen döner.
    private static VehiclePricingRequest StandardVehicle(InsuranceBranch branch = InsuranceBranch.Kasko) => new(
        Branch: branch,
        DriverAge: 40,
        VehicleAge: 5,
        EnginePowerHp: 100,
        City: "Nevşehir",
        NoClaimTier: 0);

    [Theory]
    [InlineData(InsuranceBranch.Kasko, 15000)]
    [InlineData(InsuranceBranch.Trafik, 6000)]
    public void CalculatePremium_Should_ReturnBasePremium_When_AllFactorsAreNeutral(InsuranceBranch branch, decimal expectedBase)
    {
        var result = _engine.CalculatePremium(StandardVehicle(branch));

        result.Branch.Should().Be(branch);
        result.BasePremium.Should().Be(expectedBase);
        result.TotalPremium.Should().Be(expectedBase);
        result.RiskScore.Should().Be(RiskScore.Low);
        result.Breakdown.Should().HaveCount(5);
    }

    [Fact]
    public void CalculatePremium_Should_ApplyYouthSurcharge_When_DriverAgeBelow25()
    {
        var request = StandardVehicle() with { DriverAge = 22 };

        var result = _engine.CalculatePremium(request);

        result.Breakdown.Should().ContainSingle(item => item.Factor == "Sürücü Yaşı")
            .Which.Multiplier.Should().Be(1.30m);
        // 15000 × 1.30 = 19500; toplam çarpan 1.30 → Medium.
        result.TotalPremium.Should().Be(19500.00m);
        result.RiskScore.Should().Be(RiskScore.Medium);
    }

    [Fact]
    public void CalculatePremium_Should_ApplyElderlySurcharge_When_DriverAgeAbove65()
    {
        var result = _engine.CalculatePremium(StandardVehicle() with { DriverAge = 70 });

        result.Breakdown.Single(item => item.Factor == "Sürücü Yaşı").Multiplier.Should().Be(1.15m);
    }

    [Theory]
    [InlineData(90, 1.00)]
    [InlineData(140, 1.10)]
    [InlineData(200, 1.25)]
    [InlineData(300, 1.45)]
    public void CalculatePremium_Should_ApplyEnginePowerTier_When_PowerVaries(int hp, decimal expectedMultiplier)
    {
        var result = _engine.CalculatePremium(StandardVehicle() with { EnginePowerHp = hp });

        result.Breakdown.Single(item => item.Factor == "Motor Gücü").Multiplier.Should().Be(expectedMultiplier);
    }

    [Fact]
    public void CalculatePremium_Should_ApplyCityRiskCoefficient_When_CityIsHighRisk()
    {
        var result = _engine.CalculatePremium(StandardVehicle() with { City = "İstanbul" });

        result.Breakdown.Single(item => item.Factor == "İl Risk Katsayısı").Multiplier.Should().Be(1.25m);
    }

    [Fact]
    public void CalculatePremium_Should_UseDefaultCoefficient_When_CityIsUnknown()
    {
        var result = _engine.CalculatePremium(StandardVehicle() with { City = "BilinmeyenŞehir" });

        result.Breakdown.Single(item => item.Factor == "İl Risk Katsayısı").Multiplier.Should().Be(1.00m);
    }

    [Fact]
    public void CalculatePremium_Should_ApplyNoClaimDiscount_When_TierIsPositive()
    {
        var result = _engine.CalculatePremium(StandardVehicle() with { NoClaimTier = 4 });

        result.Breakdown.Single(item => item.Factor == "Hasarsızlık İndirimi").Multiplier.Should().Be(0.80m);
        // 15000 × 0.80 = 12000; toplam çarpan 0.80 → Low.
        result.TotalPremium.Should().Be(12000.00m);
        result.RiskScore.Should().Be(RiskScore.Low);
    }

    [Fact]
    public void CalculatePremium_Should_CapNoClaimDiscount_When_TierExceedsMaximum()
    {
        var result = _engine.CalculatePremium(StandardVehicle() with { NoClaimTier = 20 });

        // En fazla 7 basamak → 1.00 − 7×0.05 = 0.65.
        result.Breakdown.Single(item => item.Factor == "Hasarsızlık İndirimi").Multiplier.Should().Be(0.65m);
        result.TotalPremium.Should().Be(9750.00m);
    }

    [Fact]
    public void CalculatePremium_Should_ReturnHighRisk_When_VehicleFactorsCompound()
    {
        var request = new VehiclePricingRequest(
            Branch: InsuranceBranch.Kasko,
            DriverAge: 22,      // 1.30
            VehicleAge: 2,      // 1.15
            EnginePowerHp: 250, // 1.45
            City: "İstanbul",   // 1.25
            NoClaimTier: 0);    // 1.00

        var result = _engine.CalculatePremium(request);

        // 1.30 × 1.15 × 1.45 × 1.25 = 2.7096875 → 15000 × 2.7096875 = 40645.3125 → 40645.31.
        result.TotalPremium.Should().Be(40645.31m);
        result.RiskScore.Should().Be(RiskScore.High);
    }

    [Fact]
    public void CalculatePremium_Should_PriceProperty_When_KonutRequestGiven()
    {
        var request = new PropertyPricingRequest(
            Branch: InsuranceBranch.Konut,
            BuildingAge: 50,    // 1.25
            SquareMeters: 250,  // 1.30
            EarthquakeZone: 1); // 1.50

        var result = _engine.CalculatePremium(request);

        result.BasePremium.Should().Be(3000m);
        result.Breakdown.Should().HaveCount(3);
        // 3000 × 1.25 × 1.30 × 1.50 = 7312.50.
        result.TotalPremium.Should().Be(7312.50m);
        result.RiskScore.Should().Be(RiskScore.High);
    }

    [Fact]
    public void CalculatePremium_Should_ApplyEarthquakeZoneCoefficient_When_ZoneVaries()
    {
        var request = new PropertyPricingRequest(InsuranceBranch.Dask, BuildingAge: 10, SquareMeters: 100, EarthquakeZone: 1);

        var result = _engine.CalculatePremium(request);

        result.BasePremium.Should().Be(1500m);
        result.Breakdown.Single(item => item.Factor == "Deprem Bölgesi").Multiplier.Should().Be(1.50m);
    }

    [Fact]
    public void CalculatePremium_Should_ApplySmokerSurcharge_When_HealthRequestDeclaresSmoker()
    {
        var request = new HealthPricingRequest(Age: 35, IsSmoker: true);

        var result = _engine.CalculatePremium(request);

        result.Branch.Should().Be(InsuranceBranch.Saglik);
        result.BasePremium.Should().Be(8000m);
        result.Breakdown.Should().HaveCount(2);
        // 8000 × 1.15 (31-45 bandı) × 1.25 (sigara) = 11500.
        result.TotalPremium.Should().Be(11500.00m);
        result.RiskScore.Should().Be(RiskScore.Medium);
    }

    [Fact]
    public void CalculatePremium_Should_ReturnHighRisk_When_HealthAgeAndSmokingCompound()
    {
        var result = _engine.CalculatePremium(new HealthPricingRequest(Age: 70, IsSmoker: true));

        // 8000 × 1.80 × 1.25 = 18000; toplam çarpan 2.25 → High.
        result.TotalPremium.Should().Be(18000.00m);
        result.RiskScore.Should().Be(RiskScore.High);
    }

    [Fact]
    public void CalculatePremium_Should_BeDeterministic_When_CalledRepeatedly()
    {
        var request = StandardVehicle() with { DriverAge = 22, EnginePowerHp = 200, City = "İzmir", NoClaimTier = 3 };

        var first = _engine.CalculatePremium(request);
        var second = _engine.CalculatePremium(request);

        second.TotalPremium.Should().Be(first.TotalPremium);
        second.RiskScore.Should().Be(first.RiskScore);
    }

    [Fact]
    public void CalculatePremium_Should_Throw_When_BranchDoesNotMatchRiskObject()
    {
        // Araç isteğine konut branşı verilirse tutarsızlık reddedilir.
        var invalid = new VehiclePricingRequest(InsuranceBranch.Konut, 40, 5, 100, "Ankara", 0);

        var act = () => _engine.CalculatePremium(invalid);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculatePremium_Should_Throw_When_PropertyRequestUsesVehicleBranch()
    {
        var invalid = new PropertyPricingRequest(InsuranceBranch.Kasko, 10, 100, 3);

        var act = () => _engine.CalculatePremium(invalid);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculatePremium_Should_Throw_When_RequestIsNull()
    {
        var act = () => _engine.CalculatePremium(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
