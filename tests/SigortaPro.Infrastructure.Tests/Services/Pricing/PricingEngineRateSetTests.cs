using FluentAssertions;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;
using SigortaPro.Infrastructure.Services.Pricing;

namespace SigortaPro.Infrastructure.Tests.Services.Pricing;

// Motor, baz primi verilen tarifeden okur; tarife verilmezse yerleşik baseline kullanılır.
// Bu, "eski teklif eski fiyatını korur / yeni teklif yeni fiyatı kullanır" garantisinin matematiksel temelidir.
public class PricingEngineRateSetTests
{
    private readonly PricingEngine _engine = new();

    // 30 yaş sürücü, 5 yaşında araç, 90 HP, katsayısız il, hasarsızlık yok → tüm çarpanlar 1.00
    // (araç yaşı 4-10 = 1.00, motor ≤100 = 1.00) → toplam prim = baz prim.
    private static VehiclePricingRequest NeutralVehicleRequest() =>
        new(InsuranceBranch.Kasko, 30, 5, 90, "Konya", 0);

    private static PricingRateSet RateSet(decimal kaskoBasePremium) =>
        new(new Dictionary<InsuranceBranch, decimal> { [InsuranceBranch.Kasko] = kaskoBasePremium });

    [Fact]
    public void CalculatePremium_Should_UseBaseline_When_NoRateSetProvided()
    {
        // Tarife yönetimi eklenmeden önceki davranış birebir korunur (Kasko baseline = 15.000).
        var result = _engine.CalculatePremium(NeutralVehicleRequest());

        result.BasePremium.Should().Be(15000m);
        result.TotalPremium.Should().Be(15000m);
    }

    [Fact]
    public void CalculatePremium_Should_UseProvidedRateSet_When_TariffChanged()
    {
        // Admin Kasko baz primini 20.000'e çıkardı → yeni fiyatlama bunu kullanır.
        var result = _engine.CalculatePremium(NeutralVehicleRequest(), RateSet(20000m));

        result.BasePremium.Should().Be(20000m);
        result.TotalPremium.Should().Be(20000m);
    }

    [Fact]
    public void CalculatePremium_Should_ReproduceOldPrice_When_PinnedRateSetIsReplayed()
    {
        // ÇEKİRDEK GARANTİ: eski teklif kendi sabitlediği tarifeyle yeniden hesaplandığında
        // güncel tarife ne olursa olsun ESKİ fiyatını birebir üretir.
        var oldTariff = RateSet(15000m);
        var newTariff = RateSet(20000m);

        var priceWhenCreated = _engine.CalculatePremium(NeutralVehicleRequest(), oldTariff).TotalPremium;
        var priceOnLaterView = _engine.CalculatePremium(NeutralVehicleRequest(), oldTariff).TotalPremium;
        var priceForNewQuote = _engine.CalculatePremium(NeutralVehicleRequest(), newTariff).TotalPremium;

        priceOnLaterView.Should().Be(priceWhenCreated, "sabitlenen tarife ile yeniden hesap aynı sonucu vermelidir");
        priceForNewQuote.Should().NotBe(priceWhenCreated, "yeni teklif güncel tarifeyi kullanmalıdır");
        priceForNewQuote.Should().Be(20000m);
    }

    [Fact]
    public void CalculatePremium_Should_FallBackToBaseline_When_RateSetLacksBranch()
    {
        // Tarife ilgili branşı içermiyorsa fiyatlama boşluğa düşmez; baseline kullanılır.
        var partialTariff = new PricingRateSet(
            new Dictionary<InsuranceBranch, decimal> { [InsuranceBranch.Saglik] = 9000m });

        var result = _engine.CalculatePremium(NeutralVehicleRequest(), partialTariff);

        result.BasePremium.Should().Be(15000m);
    }
}
