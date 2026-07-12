using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.Queries.GetRiskiestCustomers;
using SigortaPro.Application.Features.Dashboard.ReadModels;

namespace SigortaPro.Application.Tests.Features.Dashboard;

public class GetRiskiestCustomersQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardRepository = Substitute.For<IDashboardRepository>();
    private readonly GetRiskiestCustomersQueryHandler _handler;

    public GetRiskiestCustomersQueryHandlerTests()
    {
        _handler = new GetRiskiestCustomersQueryHandler(_dashboardRepository);
    }

    [Fact]
    public async Task Handle_Should_MapAggregatesToSegmentDtos_When_SegmentsExist()
    {
        var customerId = Guid.NewGuid();
        _dashboardRepository.GetRiskiestCustomerSegmentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new CustomerRiskAggregate(customerId, "Ayşe", "Yılmaz", 3, 15000m) });

        var result = await _handler.Handle(new GetRiskiestCustomersQuery(10), CancellationToken.None);

        var segment = result.Should().ContainSingle().Subject;
        segment.CustomerId.Should().Be(customerId);
        segment.FullName.Should().Be("Ayşe Yılmaz");
        segment.ClaimCount.Should().Be(3);
        segment.TotalClaimAmount.Should().Be(15000m);
    }

    [Fact]
    public async Task Handle_Should_PassRequestedTopToRepository_When_Invoked()
    {
        _dashboardRepository.GetRiskiestCustomerSegmentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerRiskAggregate>());

        await _handler.Handle(new GetRiskiestCustomersQuery(5), CancellationToken.None);

        await _dashboardRepository.Received(1).GetRiskiestCustomerSegmentsAsync(5, Arg.Any<CancellationToken>());
    }
}
