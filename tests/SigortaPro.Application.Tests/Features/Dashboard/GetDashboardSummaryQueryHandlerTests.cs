using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardRepository = Substitute.For<IDashboardRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly GetDashboardSummaryQueryHandler _handler;

    public GetDashboardSummaryQueryHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc));
        _dashboardRepository.GetMonthlySalesTrendAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MonthlySalesAggregate>());
        _dashboardRepository.GetBranchDistributionAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BranchDistributionAggregate>());

        _handler = new GetDashboardSummaryQueryHandler(_dashboardRepository, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_ComputeRatiosAndMapMetrics_When_DataExists()
    {
        _dashboardRepository.GetTotalPremiumProductionAsync(Arg.Any<CancellationToken>()).Returns(12000m);
        _dashboardRepository.GetActivePolicyCountAsync(Arg.Any<CancellationToken>()).Returns(5);
        _dashboardRepository.GetPendingQuoteCountAsync(Arg.Any<CancellationToken>()).Returns(3);
        _dashboardRepository.GetPendingClaimCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        _dashboardRepository.GetTotalPaidClaimAmountAsync(Arg.Any<CancellationToken>()).Returns(3000m);
        _dashboardRepository.GetRenewalOfferedCountAsync(Arg.Any<CancellationToken>()).Returns(4);
        _dashboardRepository.GetAcceptedRenewalCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        _dashboardRepository.GetMonthlySalesTrendAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new MonthlySalesAggregate(2026, 7, 5, 12000m) });
        _dashboardRepository.GetBranchDistributionAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new BranchDistributionAggregate(InsuranceBranch.Kasko, 5, 12000m) });

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.TotalPremiumProduction.Should().Be(12000m);
        result.ActivePolicyCount.Should().Be(5);
        result.PendingQuoteCount.Should().Be(3);
        result.PendingClaimCount.Should().Be(2);
        result.RenewalRate.Should().Be(0.25m);          // 1 / 4
        result.ClaimToPremiumRatio.Should().Be(0.25m);  // 3000 / 12000
        result.MonthlySales.Should().ContainSingle().Which.PolicyCount.Should().Be(5);
        result.BranchDistribution.Should().ContainSingle().Which.Branch.Should().Be(InsuranceBranch.Kasko);
    }

    [Fact]
    public async Task Handle_Should_ReturnZeroRatios_When_NoPremiumOrRenewals()
    {
        _dashboardRepository.GetTotalPremiumProductionAsync(Arg.Any<CancellationToken>()).Returns(0m);
        _dashboardRepository.GetTotalPaidClaimAmountAsync(Arg.Any<CancellationToken>()).Returns(0m);
        _dashboardRepository.GetRenewalOfferedCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        _dashboardRepository.GetAcceptedRenewalCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.RenewalRate.Should().Be(0m);
        result.ClaimToPremiumRatio.Should().Be(0m);
        result.MonthlySales.Should().BeEmpty();
        result.BranchDistribution.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_RequestTrendFromLast12Months_When_Invoked()
    {
        // 2026-07 referansında son 12 ay penceresi 2025-08-01'de başlar (cari ay dahil).
        await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        await _dashboardRepository.Received(1).GetMonthlySalesTrendAsync(
            new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc), Arg.Any<CancellationToken>());
    }
}
