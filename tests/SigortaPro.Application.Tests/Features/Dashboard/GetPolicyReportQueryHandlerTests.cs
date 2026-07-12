using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.Queries.GetPolicyReport;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Dashboard;

public class GetPolicyReportQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardRepository = Substitute.For<IDashboardRepository>();
    private readonly GetPolicyReportQueryHandler _handler;

    private readonly DateTime _from = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _to = new(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    public GetPolicyReportQueryHandlerTests()
    {
        _handler = new GetPolicyReportQueryHandler(_dashboardRepository);
    }

    [Fact]
    public async Task Handle_Should_MapPagedPoliciesToReportItems_When_PoliciesInRange()
    {
        var policy = new Policy("POL-2026-000001", Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc), 24750m);

        _dashboardRepository.GetPoliciesByDateRangeAsync(
                _from, _to, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Policy>(new[] { policy }, 1, 20, 1));

        var result = await _handler.Handle(new GetPolicyReportQuery(_from, _to), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        var item = result.Items.Single();
        item.PolicyNumber.Should().Be("POL-2026-000001");
        item.Status.Should().Be(PolicyStatus.Active);
        item.TotalPremium.Should().Be(24750m);
        // Müşteri/teklif navigation'ı yüklenmediğinde ad boş, branş varsayılan döner (map guard'ı).
        item.CustomerFullName.Should().BeEmpty();

        await _dashboardRepository.Received(1).GetPoliciesByDateRangeAsync(
            _from, _to, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }
}
