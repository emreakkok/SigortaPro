using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard;
using SigortaPro.Application.Features.Dashboard.DTOs;
using SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Dashboard;

// ADR-052: Operasyon dashboard'ı. En kritik davranışlar: (1) önceki dönem EŞİT UZUNLUKTA ve hemen önce,
// (2) payda 0 iken oran/değişim **null** (yanıltıcı "%0"/"+%100" üretilmez), (3) kova genişliği aralıktan türer,
// (4) huni monoton azalır ve dönüşüm gerçek kohorttan hesaplanır.
public class GetDashboardSummaryQueryHandlerTests
{
    private readonly IDashboardRepository _repository = Substitute.For<IDashboardRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetDashboardSummaryQueryHandler _handler;

    private static readonly DateTime Now = new(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);

    public GetDashboardSummaryQueryHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);

        // Varsayılanlar: NSubstitute aksi hâlde null Task döndürür.
        _repository.GetPeriodStatsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(0, 0, 0, 0, 0m));
        _repository.GetQuoteFunnelAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new QuoteFunnelAggregate(0, 0, 0, 0, 0));
        _repository.GetBranchPerformanceAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BranchPerformanceAggregate>());
        _repository.GetClaimStatusBreakdownAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClaimStatusCountAggregate>());
        _repository.GetPremiumSeriesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<PremiumGranularity>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PremiumSeriesAggregate>());
        _repository.GetRenewalCountsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((0, 0));

        // Varsayılan: çağıran Admin → finansal alanlar dolu (mevcut testlerin beklentisi). Maskeleme
        // davranışı ayrı testte Personel (Admin değil) olarak sınanır (P1 kararı D1).
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        _handler = new GetDashboardSummaryQueryHandler(_repository, _dateTimeProvider, _currentUserService);
    }

    private Task<DashboardSummaryDto> HandleAsync(DateTime? from = null, DateTime? to = null) =>
        _handler.Handle(new GetDashboardSummaryQuery(from, to), CancellationToken.None);

    // --- Tarih aralığı ve dönem normalizasyonu ---

    [Fact]
    public async Task Handle_Should_DefaultToLast30Days_When_RangeOmitted()
    {
        var result = await HandleAsync();

        result.To.Should().Be(Now);
        result.From.Should().Be(Now.AddDays(-30));
    }

    [Fact]
    public async Task Handle_Should_UsePreviousPeriodOfEqualLengthImmediatelyBefore()
    {
        // "Bu hafta" (7 gün) → önceki dönem tam 7 gün ve hemen öncesinde, örtüşmeden.
        var from = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);

        await HandleAsync(from, to);

        var expectedPreviousTo = from.AddTicks(-1);
        var expectedPreviousFrom = expectedPreviousTo - (to - from);

        await _repository.Received(1).GetPeriodStatsAsync(from, to, Arg.Any<CancellationToken>());
        await _repository.Received(1).GetPeriodStatsAsync(
            expectedPreviousFrom, expectedPreviousTo, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, PremiumGranularity.Hourly)]    // Bugün → saatlik (tek noktalı grafik olmasın)
    [InlineData(7, PremiumGranularity.Daily)]     // Bu hafta → günlük
    [InlineData(30, PremiumGranularity.Daily)]    // Son 30 gün → günlük
    [InlineData(365, PremiumGranularity.Monthly)] // Uzun aralık → aylık
    public async Task Handle_Should_DeriveGranularityFromRangeLength(int days, PremiumGranularity expected)
    {
        var to = Now;
        var from = to.AddDays(-days);

        var result = await HandleAsync(from, to);

        result.Granularity.Should().Be((PremiumGranularityDto)expected);
        await _repository.Received(1).GetPremiumSeriesAsync(from, to, expected, Arg.Any<CancellationToken>());
    }

    // --- Karşılaştırma (delta) ---

    [Fact]
    public async Task Handle_Should_ComputeDeltasAgainstPreviousPeriod()
    {
        var from = Now.AddDays(-7);
        _repository.GetPeriodStatsAsync(from, Now, Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(12, 20, 6, 3, 11800m));
        _repository.GetPeriodStatsAsync(
                Arg.Is<DateTime>(d => d < from), Arg.Is<DateTime>(d => d < Now), Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(10, 16, 4, 2, 10000m));

        var result = await HandleAsync(from, Now);

        result.Current.PremiumProduction.Should().Be(11800m);
        result.Previous.PremiumProduction.Should().Be(10000m);
        result.Deltas.PremiumProduction.Should().Be(0.18m);   // (11800-10000)/10000
        result.Deltas.NewPolicies.Should().Be(0.5m);          // (6-4)/4
        result.Deltas.NewCustomers.Should().Be(0.2m);         // (12-10)/10
    }

    [Fact]
    public async Task Handle_Should_ReturnNullDelta_When_PreviousPeriodIsZero()
    {
        // Sıfırdan artışta oran TANIMSIZDIR; "+%100" göstermek yanıltıcı olurdu.
        var from = Now.AddDays(-7);
        _repository.GetPeriodStatsAsync(from, Now, Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(5, 5, 5, 5, 5000m));
        _repository.GetPeriodStatsAsync(
                Arg.Is<DateTime>(d => d < from), Arg.Is<DateTime>(d => d < Now), Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(0, 0, 0, 0, 0m));

        var result = await HandleAsync(from, Now);

        result.Deltas.PremiumProduction.Should().BeNull();
        result.Deltas.NewPolicies.Should().BeNull();
        result.Deltas.NewQuotes.Should().BeNull();
        result.Deltas.NewCustomers.Should().BeNull();
    }

    // --- Satış hunisi ve dönüşüm ---

    [Fact]
    public async Task Handle_Should_BuildMonotonicFunnelAndConversion()
    {
        // 10 fiyatlandı + 5 onaylandı + 4 satın alındı + 2 süresi doldu + 1 reddedildi = 22 oluşturulan.
        _repository.GetQuoteFunnelAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new QuoteFunnelAggregate(Priced: 10, Approved: 5, Purchased: 4, Expired: 2, Rejected: 1));

        var result = await HandleAsync();

        result.Funnel.Created.Should().Be(22);
        // Satın alma onaydan geçer → "onaylanan" adımı satın alınanları da içerir (huni monoton azalır).
        result.Funnel.Approved.Should().Be(9);
        result.Funnel.Purchased.Should().Be(4);
        result.Funnel.Created.Should().BeGreaterThanOrEqualTo(result.Funnel.Approved);
        result.Funnel.Approved.Should().BeGreaterThanOrEqualTo(result.Funnel.Purchased);
        result.Funnel.ConversionRate.Should().Be(0.1818m); // 4 / 22
    }

    [Fact]
    public async Task Handle_Should_ReturnNullConversion_When_NoQuotesInPeriod()
    {
        var result = await HandleAsync();

        result.Funnel.Created.Should().Be(0);
        result.Funnel.ConversionRate.Should().BeNull("teklif yokken dönüşüm oranı tanımsızdır");
    }

    // --- Branş performansı ---

    [Fact]
    public async Task Handle_Should_MapBranchPerformanceWithConversion()
    {
        _repository.GetBranchPerformanceAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new BranchPerformanceAggregate(InsuranceBranch.Saglik, QuoteCount: 20, PurchasedCount: 5, PremiumTotal: 120000m),
                new BranchPerformanceAggregate(InsuranceBranch.Kasko, QuoteCount: 0, PurchasedCount: 0, PremiumTotal: 0m),
            });

        var result = await HandleAsync();

        var saglik = result.BranchPerformance.Single(b => b.Branch == InsuranceBranch.Saglik);
        saglik.ConversionRate.Should().Be(0.25m); // 5 / 20
        saglik.PremiumTotal.Should().Be(120000m);

        // Teklif yoksa dönüşüm tanımsızdır (%0 göstermek yanıltıcı olurdu).
        result.BranchPerformance.Single(b => b.Branch == InsuranceBranch.Kasko)
            .ConversionRate.Should().BeNull();
    }

    // --- Hasar operasyonu ---

    [Fact]
    public async Task Handle_Should_MapClaimBreakdown_UsingOnlyPaidRecordsForPaidAmount()
    {
        _repository.GetClaimStatusBreakdownAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ClaimStatusCountAggregate(ClaimStatus.Submitted, 3, EstimatedTotal: 9000m, ApprovedTotal: 0m),
                new ClaimStatusCountAggregate(ClaimStatus.UnderReview, 2, EstimatedTotal: 6000m, ApprovedTotal: 0m),
                new ClaimStatusCountAggregate(ClaimStatus.Approved, 1, EstimatedTotal: 4000m, ApprovedTotal: 3500m),
                new ClaimStatusCountAggregate(ClaimStatus.Paid, 4, EstimatedTotal: 12000m, ApprovedTotal: 10000m),
            });

        var result = await HandleAsync();

        result.Claims.Submitted.Should().Be(3);
        result.Claims.UnderReview.Should().Be(2);
        result.Claims.Approved.Should().Be(1);
        result.Claims.Paid.Should().Be(4);
        // Ödenen tutar YALNIZCA Paid kayıtlardan gelir — onaylanmış ama ödenmemiş tutar dahil edilmez.
        result.Claims.PaidAmount.Should().Be(10000m);
        result.Claims.EstimatedAmount.Should().Be(31000m);
    }

    // --- Oranlar: payda 0 iken null ---

    [Fact]
    public async Task Handle_Should_ComputePeriodRenewalRate()
    {
        _repository.GetRenewalCountsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((Offered: 8, Accepted: 3));

        var result = await HandleAsync();

        result.RenewalRate.Should().Be(0.375m); // 3 / 8
    }

    [Fact]
    public async Task Handle_Should_ReturnNullRenewalRate_When_NoRenewalOfferedInPeriod()
    {
        var result = await HandleAsync();

        result.RenewalRate.Should().BeNull("dönemde yenileme sunulmadıysa oran tanımsızdır");
    }

    [Fact]
    public async Task Handle_Should_ReturnNullLossRatio_When_NoPremiumProduced()
    {
        _repository.GetTotalPremiumProductionAsync(Arg.Any<CancellationToken>()).Returns(0m);
        _repository.GetTotalPaidClaimAmountAsync(Arg.Any<CancellationToken>()).Returns(0m);

        var result = await HandleAsync();

        result.Portfolio.LossRatio.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ComputeCumulativeLossRatio()
    {
        _repository.GetTotalPremiumProductionAsync(Arg.Any<CancellationToken>()).Returns(120000m);
        _repository.GetTotalPaidClaimAmountAsync(Arg.Any<CancellationToken>()).Returns(30000m);

        var result = await HandleAsync();

        result.Portfolio.LossRatio.Should().Be(0.25m);
    }

    // --- Aksiyon merkezi ---

    [Fact]
    public async Task Handle_Should_ExposeOperationalAlerts_WithRenewalWindow()
    {
        _repository.GetPendingQuoteCountAsync(Arg.Any<CancellationToken>()).Returns(18);
        _repository.GetPendingClaimCountAsync(Arg.Any<CancellationToken>()).Returns(5);
        _repository.GetUpcomingRenewalCountAsync(Now, 30, Arg.Any<CancellationToken>()).Returns(7);
        _repository.GetFailedPaymentCountAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var result = await HandleAsync();

        result.Alerts.PendingQuotes.Should().Be(18);
        result.Alerts.PendingClaims.Should().Be(5);
        result.Alerts.UpcomingRenewals.Should().Be(7);
        result.Alerts.UpcomingRenewalWindowDays.Should().Be(30);
        result.Alerts.FailedPayments.Should().Be(2);
    }

    // --- Finansal görünürlük ayrımı (P1 kararı D1) ---

    [Fact]
    public async Task Handle_Should_MaskFinancialFields_But_KeepOperational_When_CallerIsNotAdmin()
    {
        // Personel (Admin değil): agregat finansal alanlar backend response'unda null; operasyonel alanlar dolu.
        _currentUserService.IsInRole(Roles.Admin).Returns(false);

        var from = Now.AddDays(-7);
        _repository.GetPeriodStatsAsync(from, Now, Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(12, 20, 6, 3, 11800m));
        _repository.GetPeriodStatsAsync(
                Arg.Is<DateTime>(d => d < from), Arg.Is<DateTime>(d => d < Now), Arg.Any<CancellationToken>())
            .Returns(new PeriodStatsAggregate(10, 16, 4, 2, 10000m));
        _repository.GetBranchPerformanceAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new BranchPerformanceAggregate(InsuranceBranch.Saglik, 20, 5, 120000m) });
        _repository.GetClaimStatusBreakdownAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ClaimStatusCountAggregate(ClaimStatus.Paid, 4, 12000m, 10000m) });
        _repository.GetPremiumSeriesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<PremiumGranularity>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new PremiumSeriesAggregate(from, 6, 11800m) });
        _repository.GetFailedPaymentCountAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(2);
        _repository.GetTotalPremiumProductionAsync(Arg.Any<CancellationToken>()).Returns(500000m);
        _repository.GetTotalPaidClaimAmountAsync(Arg.Any<CancellationToken>()).Returns(80000m);
        _repository.GetActivePolicyCountAsync(Arg.Any<CancellationToken>()).Returns(42);
        _repository.GetTotalCustomerCountAsync(Arg.Any<CancellationToken>()).Returns(37);

        var result = await HandleAsync(from, Now);

        // Finansal alanlar maskeli (null).
        result.Current.PremiumProduction.Should().BeNull();
        result.Previous.PremiumProduction.Should().BeNull();
        result.Deltas.PremiumProduction.Should().BeNull();
        result.Alerts.FailedPayments.Should().BeNull();
        result.Portfolio.LifetimePremiumProduction.Should().BeNull();
        result.Portfolio.PaidClaimAmount.Should().BeNull();
        result.Portfolio.LossRatio.Should().BeNull();
        result.PremiumSeries.Should().OnlyContain(point => point.PremiumTotal == null);
        result.BranchPerformance.Should().OnlyContain(branch => branch.PremiumTotal == null);
        result.Claims.PaidAmount.Should().BeNull();
        result.Claims.EstimatedAmount.Should().BeNull();

        // Operasyonel alanlar korunur.
        result.Current.NewPolicies.Should().Be(6);
        result.Current.NewCustomers.Should().Be(12);
        result.Deltas.NewPolicies.Should().Be(0.5m);
        result.Alerts.PendingQuotes.Should().Be(0);
        result.Portfolio.ActivePolicyCount.Should().Be(42);
        result.Portfolio.TotalCustomerCount.Should().Be(37);
        result.PremiumSeries.Should().OnlyContain(point => point.PolicyCount == 6);
        result.BranchPerformance.Single().ConversionRate.Should().Be(0.25m);
        result.Claims.Paid.Should().Be(4);
    }

    [Fact]
    public async Task Handle_Should_ReturnFinancialFields_When_CallerIsAdmin()
    {
        // Admin: finansal alanlar dolu (bit-aynı davranış korunur).
        _currentUserService.IsInRole(Roles.Admin).Returns(true);
        _repository.GetTotalPremiumProductionAsync(Arg.Any<CancellationToken>()).Returns(500000m);

        var result = await HandleAsync();

        result.Portfolio.LifetimePremiumProduction.Should().Be(500000m);
    }
}
