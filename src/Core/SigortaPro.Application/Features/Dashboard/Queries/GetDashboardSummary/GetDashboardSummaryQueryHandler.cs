using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.DTOs;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    // Aralık verilmezse varsayılan pencere.
    private const int DefaultRangeDays = 30;

    // "Yaklaşan yenileme" ufku — bu süre içinde bitecek aktif poliçeler aksiyon listesine düşer.
    private const int UpcomingRenewalWindowDays = 30;

    // Zaman serisi kova genişliği eşikleri: tek günde saatlik (tek noktalı grafik olmasın),
    // ~3 aya kadar günlük, ötesi aylık (yüzlerce nokta dönmesin).
    private const double HourlyMaxDays = 2;
    private const double DailyMaxDays = 92;

    private readonly IDashboardRepository _dashboardRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardSummaryQueryHandler(
        IDashboardRepository dashboardRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _dashboardRepository = dashboardRepository;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var to = request.To ?? now;
        var from = request.From ?? to.AddDays(-DefaultRangeDays);

        // Önceki dönem: SEÇİLEN ARALIKLA EŞİT UZUNLUKTA ve hemen öncesinde, örtüşmeyecek şekilde.
        // (Örn. "bu hafta" → önceki 7 gün; "bugün" → dün.) Böylece karşılaştırma doğru normalize edilir.
        var length = to - from;
        var previousTo = from.AddTicks(-1);
        var previousFrom = previousTo - length;

        var granularity = ResolveGranularity(length);

        var current = await _dashboardRepository.GetPeriodStatsAsync(from, to, cancellationToken);
        var previous = await _dashboardRepository.GetPeriodStatsAsync(previousFrom, previousTo, cancellationToken);

        var funnel = await _dashboardRepository.GetQuoteFunnelAsync(from, to, cancellationToken);
        var branchPerformance = await _dashboardRepository.GetBranchPerformanceAsync(from, to, cancellationToken);
        var claimBreakdown = await _dashboardRepository.GetClaimStatusBreakdownAsync(from, to, cancellationToken);
        var premiumSeries = await _dashboardRepository.GetPremiumSeriesAsync(from, to, granularity, cancellationToken);
        var (renewalsOffered, renewalsAccepted) = await _dashboardRepository.GetRenewalCountsAsync(from, to, cancellationToken);

        // Aksiyon merkezi: açık iş yükü dönemden bağımsızdır (bekleyen teklif/hasar, yaklaşan yenileme);
        // başarısız ödeme ise seçilen aralığa göredir.
        var pendingQuotes = await _dashboardRepository.GetPendingQuoteCountAsync(cancellationToken);
        var pendingClaims = await _dashboardRepository.GetPendingClaimCountAsync(cancellationToken);
        var upcomingRenewals = await _dashboardRepository.GetUpcomingRenewalCountAsync(
            now, UpcomingRenewalWindowDays, cancellationToken);
        var failedPayments = await _dashboardRepository.GetFailedPaymentCountAsync(from, to, cancellationToken);

        // Portföy: anlık durum (dönemden bağımsız). Hasar/prim oranı kümülatiftir — sigortacılıkta anlamlı olan budur.
        var activePolicies = await _dashboardRepository.GetActivePolicyCountAsync(cancellationToken);
        var totalCustomers = await _dashboardRepository.GetTotalCustomerCountAsync(cancellationToken);
        var lifetimePremium = await _dashboardRepository.GetTotalPremiumProductionAsync(cancellationToken);
        var paidClaimAmount = await _dashboardRepository.GetTotalPaidClaimAmountAsync(cancellationToken);

        var summary = new DashboardSummaryDto(
            From: from,
            To: to,
            Granularity: (PremiumGranularityDto)granularity,
            Current: ToStatsDto(current),
            Previous: ToStatsDto(previous),
            Deltas: new DashboardDeltaDto(
                PremiumProduction: Change(current.PremiumProduction, previous.PremiumProduction),
                NewPolicies: Change(current.NewPolicies, previous.NewPolicies),
                NewQuotes: Change(current.NewQuotes, previous.NewQuotes),
                NewCustomers: Change(current.NewCustomers, previous.NewCustomers)),
            Alerts: new OperationalAlertsDto(
                pendingQuotes, pendingClaims, upcomingRenewals, UpcomingRenewalWindowDays, failedPayments),
            Portfolio: new PortfolioDto(
                activePolicies,
                totalCustomers,
                lifetimePremium,
                paidClaimAmount,
                DashboardMappings.Ratio(paidClaimAmount, lifetimePremium)),
            Funnel: BuildFunnel(funnel),
            PremiumSeries: premiumSeries.Select(DashboardMappings.ToPointDto).ToList(),
            BranchPerformance: branchPerformance.Select(DashboardMappings.ToPerformanceDto).ToList(),
            Claims: BuildClaims(claimBreakdown),
            RenewalRate: DashboardMappings.Ratio(renewalsAccepted, renewalsOffered));

        // P1 kararı D1 (finansal görünürlük ayrımı): agregat finansal alanlar yalnızca Admin'e döner.
        // Admin yolu bit-aynı kalır (aynı DTO); Personel için finansallar backend'de null maskelenir
        // (yalnızca frontend gizleme yeterli değildir — veri response'a hiç yazılmaz). Operasyonel alanlar korunur.
        return _currentUserService.IsInRole(Roles.Admin) ? summary : MaskFinancials(summary);
    }

    // Agregat finansal alanları null'a çeker (kayıt-başına prim değil — o operasyoneldir ve bu DTO'da yoktur).
    private static DashboardSummaryDto MaskFinancials(DashboardSummaryDto summary) => summary with
    {
        Current = summary.Current with { PremiumProduction = null },
        Previous = summary.Previous with { PremiumProduction = null },
        Deltas = summary.Deltas with { PremiumProduction = null },
        Alerts = summary.Alerts with { FailedPayments = null },
        Portfolio = summary.Portfolio with
        {
            LifetimePremiumProduction = null,
            PaidClaimAmount = null,
            LossRatio = null,
        },
        PremiumSeries = summary.PremiumSeries.Select(point => point with { PremiumTotal = null }).ToList(),
        BranchPerformance = summary.BranchPerformance
            .Select(branch => branch with { PremiumTotal = null })
            .ToList(),
        Claims = summary.Claims with { PaidAmount = null, EstimatedAmount = null },
    };

    private static PremiumGranularity ResolveGranularity(TimeSpan length) => length.TotalDays switch
    {
        <= HourlyMaxDays => PremiumGranularity.Hourly,
        <= DailyMaxDays => PremiumGranularity.Daily,
        _ => PremiumGranularity.Monthly,
    };

    private static DashboardPeriodStatsDto ToStatsDto(PeriodStatsAggregate stats) => new(
        stats.NewCustomers, stats.NewQuotes, stats.NewPolicies, stats.NewClaims, stats.PremiumProduction);

    /// <summary>
    /// Önceki döneme göre oransal değişim. Önceki dönem 0 ise <c>null</c> döner — sıfırdan artışı
    /// "+%100" göstermek yanıltıcı olurdu (artış oranı tanımsızdır).
    /// </summary>
    private static decimal? Change(decimal current, decimal previous) =>
        previous == 0m ? null : Math.Round((current - previous) / previous, 4, MidpointRounding.AwayFromZero);

    // Huni monoton azalmalıdır: satın alma onaydan geçtiğinden "onaylanan" adımı satın alınanları da içerir.
    private static QuoteFunnelDto BuildFunnel(QuoteFunnelAggregate funnel)
    {
        var created = funnel.Priced + funnel.Approved + funnel.Purchased + funnel.Expired + funnel.Rejected;
        var approved = funnel.Approved + funnel.Purchased;

        return new QuoteFunnelDto(
            Created: created,
            Approved: approved,
            Purchased: funnel.Purchased,
            Expired: funnel.Expired,
            Rejected: funnel.Rejected,
            ConversionRate: DashboardMappings.Ratio(funnel.Purchased, created));
    }

    private static ClaimOperationDto BuildClaims(IReadOnlyList<ClaimStatusCountAggregate> breakdown)
    {
        int CountOf(ClaimStatus status) =>
            breakdown.FirstOrDefault(row => row.Status == status)?.Count ?? 0;

        return new ClaimOperationDto(
            Submitted: CountOf(ClaimStatus.Submitted),
            UnderReview: CountOf(ClaimStatus.UnderReview),
            Approved: CountOf(ClaimStatus.Approved),
            Rejected: CountOf(ClaimStatus.Rejected),
            Paid: CountOf(ClaimStatus.Paid),
            // Yalnızca ÖDENMİŞ hasarların onay tutarı — güvenilir olan tek ödeme rakamı.
            PaidAmount: breakdown.FirstOrDefault(row => row.Status == ClaimStatus.Paid)?.ApprovedTotal ?? 0m,
            // Tahmini tutar bir BEYANDIR; onaylanan tutarla karıştırılmaz.
            EstimatedAmount: breakdown.Sum(row => row.EstimatedTotal));
    }
}
