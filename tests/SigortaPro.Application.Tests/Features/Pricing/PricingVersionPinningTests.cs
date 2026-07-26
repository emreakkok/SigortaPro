using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Pricing;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Pricing;

// ADR-048'in ÇEKİRDEK GARANTİSİ: admin tarifeyi değiştirdiğinde
//   • yeni teklifler yeni tarifeyi kullanır,
//   • mevcut teklif/poliçeler SABİTLEDİKLERİ tarifeyle hesaplanmaya devam eder (fiyatları değişmez).
// Bu testler garantiyi çözümleyici (resolver) düzeyinde kanıtlar; fiyatın gerçekten değiştiğini/
// değişmediğini motor düzeyinde PricingEngineRateSetTests doğrular.
public class PricingVersionPinningTests
{
    private static readonly DateTime Now = new(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly IPricingVersionRepository _repository = Substitute.For<IPricingVersionRepository>();
    private readonly PricingRateResolver _resolver;

    public PricingVersionPinningTests()
    {
        _resolver = new PricingRateResolver(_repository);
    }

    private static PricingVersion Version(int number, DateTime effectiveFrom, decimal kaskoBasePremium)
    {
        var version = new PricingVersion(number, effectiveFrom, null, Guid.NewGuid(), "Admin");
        foreach (var branch in Enum.GetValues<InsuranceBranch>())
        {
            version.SetRate(branch, branch == InsuranceBranch.Kasko ? kaskoBasePremium : 1000m);
        }

        return version;
    }

    [Fact]
    public async Task ResolveEffective_Should_ReturnNewestEffectiveVersion_When_TariffChanged()
    {
        // Admin yeni tarife yayınladı → YENİ fiyatlamalar bunu kullanmalıdır.
        var newVersion = Version(2, Now.AddDays(-1), 20000m);
        _repository.GetEffectiveAsync(Now, Arg.Any<CancellationToken>()).Returns(newVersion);

        var effective = await _resolver.ResolveEffectiveAsync(Now);

        effective.VersionId.Should().Be(newVersion.Id);
        effective.Rates!.BasePremiumFor(InsuranceBranch.Kasko).Should().Be(20000m);
    }

    [Fact]
    public async Task ResolveForQuote_Should_ReturnPinnedVersionRates_NotTheNewOne()
    {
        // Eski teklif v1'i sabitlemişti; tarife v2'ye geçse bile teklif v1 oranlarıyla hesaplanır.
        var oldVersion = Version(1, Now.AddYears(-1), 15000m);
        _repository.GetWithRatesByIdAsync(oldVersion.Id, Arg.Any<CancellationToken>()).Returns(oldVersion);

        var rates = await _resolver.ResolveForQuoteAsync(oldVersion.Id);

        rates.Should().NotBeNull();
        rates!.BasePremiumFor(InsuranceBranch.Kasko).Should().Be(15000m,
            "teklifin sabitlediği tarife, sonraki tarife değişikliklerinden etkilenmemelidir");
    }

    [Fact]
    public async Task ResolveForQuote_Should_FallBackToBaseline_When_QuoteHasNoPinnedVersion()
    {
        // Tarife yönetimi öncesi oluşturulmuş teklifler → yerleşik baseline (bit-aynı sonuç).
        var rates = await _resolver.ResolveForQuoteAsync(null);

        rates.Should().BeNull();
        await _repository.DidNotReceiveWithAnyArgs().GetWithRatesByIdAsync(default, default);
    }

    [Fact]
    public async Task ResolveEffective_Should_FallBackToBaseline_When_NoVersionExists()
    {
        _repository.GetEffectiveAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((PricingVersion?)null);

        var effective = await _resolver.ResolveEffectiveAsync(Now);

        effective.Should().Be(EffectivePricing.Baseline);
        effective.VersionId.Should().BeNull();
    }

    [Fact]
    public void Quote_Should_PinPricingVersion_OnlyWhileDraft()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Saglik, null, null);
        var versionId = Guid.NewGuid();

        quote.PinPricingVersion(versionId);
        quote.PricingVersionId.Should().Be(versionId);

        // Fiyatlandıktan sonra sabitlenen versiyon değiştirilemez → geçmiş fiyat korunur.
        quote.MarkAsPriced(1000m, Now.AddDays(30));
        var act = () => quote.PinPricingVersion(Guid.NewGuid());

        act.Should().Throw<Domain.Common.DomainException>();
        quote.PricingVersionId.Should().Be(versionId);
    }
}
