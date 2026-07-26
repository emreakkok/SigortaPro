using FluentAssertions;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;
using SigortaPro.Infrastructure.Services.Pricing;

namespace SigortaPro.Infrastructure.Tests.Services.Pricing;

// ADR-057: Kullanım amacı çarpanları (MVP simülasyonu). Hususi referans (1.00), ticari ve taksi ek prim.
// Beyanı olmayan (null) isteklerde faktör HİÇ üretilmez → eski kayıtların fiyatı/dökümü değişmez.
public class PricingEngineVehicleUsageTests
{
    private const string UsageFactorName = "Kullanım Amacı";

    private readonly PricingEngine _engine = new();

    // Nötr girdi: 30 yaş sürücü (1.00), 5 yaş araç (1.00), 100 HP (1.00), tanınmayan il (1.00), basamak 0 (1.00)
    // → toplam çarpan yalnızca kullanım amacından gelir; katsayı izole biçimde ölçülebilir.
    private static VehiclePricingRequest Request(VehicleUsage? usage) => new(
        InsuranceBranch.Kasko,
        DriverAge: 30,
        VehicleAge: 5,
        EnginePowerHp: 100,
        City: "Bilinmeyenşehir",
        NoClaimTier: 0,
        UsagePurpose: usage);

    [Theory]
    [InlineData(VehicleUsage.Hususi, 1.00)]
    [InlineData(VehicleUsage.Ticari, 1.30)]
    [InlineData(VehicleUsage.Taksi, 1.60)]
    public void CalculatePremium_Should_ApplyUsageCoefficient(VehicleUsage usage, decimal expected)
    {
        var result = _engine.CalculatePremium(Request(usage));

        var factor = result.Breakdown.Single(item => item.Factor == UsageFactorName);
        factor.Multiplier.Should().Be(expected);

        // Kasko baz primi 15.000; diğer tüm faktörler nötr olduğundan toplam = baz × kullanım katsayısı.
        result.TotalPremium.Should().Be(15000m * expected);
    }

    [Fact]
    public void CalculatePremium_Should_IncreasePremium_When_UsageBecomesCommercialOrTaxi()
    {
        var hususi = _engine.CalculatePremium(Request(VehicleUsage.Hususi)).TotalPremium;
        var ticari = _engine.CalculatePremium(Request(VehicleUsage.Ticari)).TotalPremium;
        var taksi = _engine.CalculatePremium(Request(VehicleUsage.Taksi)).TotalPremium;

        ticari.Should().BeGreaterThan(hususi);
        taksi.Should().BeGreaterThan(ticari);
    }

    [Fact]
    public void CalculatePremium_Should_NotApplyOrShowUsageFactor_When_DeclarationMissing()
    {
        // Beyanı olmayan (eski) kayıt: faktör dökümde YOK ve prim, hususi ile aynı (ek prim uygulanmaz).
        var withoutDeclaration = _engine.CalculatePremium(Request(usage: null));
        var hususi = _engine.CalculatePremium(Request(VehicleUsage.Hususi));

        withoutDeclaration.Breakdown.Should().NotContain(item => item.Factor == UsageFactorName);
        withoutDeclaration.TotalPremium.Should().Be(hususi.TotalPremium);
    }

    [Fact]
    public void CalculatePremium_Should_ShowUsageFactor_EvenWhenNeutral()
    {
        // Hususi çarpanı 1.00'dır ama BEYANA dayandığından dökümde gösterilir (ADR-054 ilkesi:
        // yalnızca gerçek veriye dayanmayan faktörler gizlenir).
        var result = _engine.CalculatePremium(Request(VehicleUsage.Hususi));

        result.Breakdown.Should().Contain(item => item.Factor == UsageFactorName);
    }

    [Fact]
    public void CalculatePremium_Should_NotExposeUsageFactor_ForPropertyOrHealthBranches()
    {
        // Kullanım amacı yalnızca araç branşlarının girdisidir; Konut/DASK ve Sağlık istekleri bu alanı taşımaz.
        var property = _engine.CalculatePremium(
            new PropertyPricingRequest(InsuranceBranch.Konut, BuildingAge: 10, SquareMeters: 100, EarthquakeZone: 3));
        var health = _engine.CalculatePremium(new HealthPricingRequest(Age: 30, IsSmoker: false));

        property.Breakdown.Should().NotContain(item => item.Factor == UsageFactorName);
        health.Breakdown.Should().NotContain(item => item.Factor == UsageFactorName);
    }
}
