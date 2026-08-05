using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Pricing;

// Yenileme indirimi: yalnızca yenileme tekliflerinde uygulanır, teklifte dondurulur (deterministik yeniden hesap)
// ve prim dökümünde yalnızca etkiliyse görünür. Fiyatlama köprüsü (QuotePricingFactory) düzeyinde doğrulanır.
public class RenewalDiscountPricingTests
{
    private static IPricingEngine EngineReturning(decimal total)
    {
        var engine = Substitute.For<IPricingEngine>();
        engine.CalculatePremium(Arg.Any<PricingRequest>(), Arg.Any<PricingRateSet?>())
            .Returns(new PricingResult(
                InsuranceBranch.Saglik, total, total, RiskScore.Low, new List<PricingBreakdownItem>()));
        return engine;
    }

    private static PricingSnapshot HealthSnapshot() =>
        PricingSnapshot.ForHealth(new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc), 30, false);

    [Fact]
    public void Compute_Should_ApplyRenewalDiscount_ToTotal_AndBreakdown()
    {
        var customer = CustomerTestData.CreateCustomer(Guid.NewGuid(), Guid.NewGuid());

        var outcome = QuotePricingFactory.Compute(
            EngineReturning(8000m), InsuranceBranch.Saglik, customer, null, null,
            Enumerable.Empty<Coverage>(), CoveragePackage.Standart, DateTime.UtcNow,
            snapshot: HealthSnapshot(), renewalDiscountFactor: 0.90m);

        // 8000 × 1.00 (Standart) × 1.00 (hasar geçmişi) × 0.90 (yenileme) = 7200.
        outcome.TotalPremium.Should().Be(7200m);
        outcome.Breakdown.Should().Contain(item => item.Factor == "Yenileme İndirimi" && item.Multiplier == 0.90m);
    }

    [Fact]
    public void Compute_Should_NotAddRenewalItem_When_NoDiscount()
    {
        var customer = CustomerTestData.CreateCustomer(Guid.NewGuid(), Guid.NewGuid());

        var outcome = QuotePricingFactory.Compute(
            EngineReturning(8000m), InsuranceBranch.Saglik, customer, null, null,
            Enumerable.Empty<Coverage>(), CoveragePackage.Standart, DateTime.UtcNow,
            snapshot: HealthSnapshot());

        outcome.TotalPremium.Should().Be(8000m);
        outcome.Breakdown.Should().NotContain(item => item.Factor == "Yenileme İndirimi");
    }

    [Fact]
    public void Quote_ApplyRenewalDiscount_Should_RejectOutOfRange_AndAfterPriced()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Saglik, null, null);

        quote.Invoking(q => q.ApplyRenewalDiscount(0m)).Should().Throw<Domain.Common.DomainException>();
        quote.Invoking(q => q.ApplyRenewalDiscount(1.5m)).Should().Throw<Domain.Common.DomainException>();

        quote.ApplyRenewalDiscount(0.90m);
        quote.RenewalDiscountFactor.Should().Be(0.90m);

        // Fiyatlandıktan sonra değiştirilemez → geçmiş fiyat korunur.
        quote.MarkAsPriced(1000m, DateTime.UtcNow.AddDays(10));
        quote.Invoking(q => q.ApplyRenewalDiscount(0.80m)).Should().Throw<Domain.Common.DomainException>();
    }
}
