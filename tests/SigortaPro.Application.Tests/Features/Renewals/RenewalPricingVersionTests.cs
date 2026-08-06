using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Renewals;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Application.Tests.Features.Quotes;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Renewals;

// Yenileme teklifi AKTİF PricingVersion ile fiyatlanır (yeni dönem yeni tarifeden) ve aktif tarifenin
// yenileme indirimini teklifte DONDURUR. Kaynak (süresi dolan) teklif/poliçe DEĞİŞMEZ — geçmiş prim korunur.
public class RenewalPricingVersionTests
{
    private static readonly DateTime Now = new(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_Should_UseActiveVersion_FreezeRenewalDiscount_AndNotMutateSource()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(Guid.NewGuid(), customerId);
        var vehicle = CustomerTestData.CreateVehicle(customerId, vehicleId);
        var product = QuoteTestData.CreateProduct(InsuranceBranch.Kasko);

        // Süresi dolan (kaynak) teklif — kendi primini sabitlemiş.
        var source = new Quote(customerId, product.Id, InsuranceBranch.Kasko, vehicleId, null);
        source.SelectCoveragePackage(CoveragePackage.Standart);
        source.MarkAsPriced(12345m, Now.AddDays(-350));

        var engine = Substitute.For<IPricingEngine>();
        engine.CalculatePremium(Arg.Any<PricingRequest>(), Arg.Any<PricingRateSet?>())
            .Returns(new PricingResult(
                InsuranceBranch.Kasko, 15000m, 15000m, RiskScore.Medium, new List<PricingBreakdownItem>()));

        // AKTİF tarife: %10 yenileme indirimi taşır.
        var activeVersionId = Guid.NewGuid();
        var ruleSet = new PricingRuleSet(
            new Dictionary<CoveragePackage, decimal>(),
            new Dictionary<string, decimal>(),
            DefaultCityRiskCoefficient: 1.00m,
            RenewalDiscountFactor: 0.90m);
        var effective = new EffectivePricing(
            activeVersionId,
            new PricingRateSet(new Dictionary<InsuranceBranch, decimal> { [InsuranceBranch.Kasko] = 15000m }, ruleSet));

        var snapshot = PricingSnapshot.ForVehicle(Now, 40, 3, 132, "İstanbul", 0, VehicleUsage.Hususi);

        var renewal = RenewalQuoteFactory.Build(
            source, customer, product, vehicle, null, engine, Now, Now.AddDays(15), snapshot, effective);

        // Yenileme AKTİF versiyonu sabitler ve yenileme indirimini dondurur.
        renewal.PricingVersionId.Should().Be(activeVersionId);
        renewal.RenewalDiscountFactor.Should().Be(0.90m);
        renewal.Status.Should().Be(QuoteStatus.Priced);
        renewal.Id.Should().NotBe(source.Id);
        // 15000 × 1.00 (Standart) × 0.90 (yenileme) = 13500.
        renewal.TotalPremium.Should().Be(13500m);

        // GEÇMİŞ teklif değişmez.
        source.TotalPremium.Should().Be(12345m);
        source.RenewalDiscountFactor.Should().Be(1.00m);
    }
}
