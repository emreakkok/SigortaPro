using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    // Aylık satış trendinde gösterilecek ay sayısı (cari ay dahil son 12 ay).
    private const int TrendMonths = 12;

    private readonly IDashboardRepository _dashboardRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDashboardSummaryQueryHandler(
        IDashboardRepository dashboardRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _dashboardRepository = dashboardRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var totalPremium = await _dashboardRepository.GetTotalPremiumProductionAsync(cancellationToken);
        var activePolicyCount = await _dashboardRepository.GetActivePolicyCountAsync(cancellationToken);
        var pendingQuoteCount = await _dashboardRepository.GetPendingQuoteCountAsync(cancellationToken);
        var pendingClaimCount = await _dashboardRepository.GetPendingClaimCountAsync(cancellationToken);
        var paidClaimAmount = await _dashboardRepository.GetTotalPaidClaimAmountAsync(cancellationToken);
        var renewalOffered = await _dashboardRepository.GetRenewalOfferedCountAsync(cancellationToken);
        var renewalAccepted = await _dashboardRepository.GetAcceptedRenewalCountAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var trendStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(TrendMonths - 1));

        var monthlySales = await _dashboardRepository.GetMonthlySalesTrendAsync(trendStart, cancellationToken);
        var branchDistribution = await _dashboardRepository.GetBranchDistributionAsync(cancellationToken);

        // Türetilmiş oranlar Application'da hesaplanır (0'a bölme korunur); 4 ondalığa yuvarlanır.
        var renewalRate = renewalOffered == 0
            ? 0m
            : Math.Round((decimal)renewalAccepted / renewalOffered, 4, MidpointRounding.AwayFromZero);

        var claimToPremiumRatio = totalPremium == 0m
            ? 0m
            : Math.Round(paidClaimAmount / totalPremium, 4, MidpointRounding.AwayFromZero);

        return new DashboardSummaryDto(
            totalPremium,
            activePolicyCount,
            pendingQuoteCount,
            pendingClaimCount,
            renewalRate,
            claimToPremiumRatio,
            monthlySales.Select(DashboardMappings.ToPointDto).ToList(),
            branchDistribution.Select(DashboardMappings.ToPointDto).ToList());
    }
}
