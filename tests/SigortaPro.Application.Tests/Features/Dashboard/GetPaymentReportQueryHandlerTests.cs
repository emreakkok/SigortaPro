using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.Queries.GetPaymentReport;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Dashboard;

public class GetPaymentReportQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardRepository = Substitute.For<IDashboardRepository>();
    private readonly GetPaymentReportQueryHandler _handler;

    private readonly DateTime _from = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _to = new(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    public GetPaymentReportQueryHandlerTests()
    {
        _handler = new GetPaymentReportQueryHandler(_dashboardRepository);
    }

    [Fact]
    public async Task Handle_Should_MapPagedPaymentsToReportItems_When_PaymentsInRange()
    {
        var customerId = Guid.NewGuid();
        var payment = new Payment(customerId, Guid.NewGuid(), 24750m, 3, "************1111",
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        payment.MarkSuccessful("POS-REF");

        _dashboardRepository.GetPaymentsByDateRangeAsync(
                _from, _to, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Payment>(new[] { payment }, 1, 20, 1));

        var result = await _handler.Handle(new GetPaymentReportQuery(_from, _to), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        var item = result.Items.Single();
        item.CustomerId.Should().Be(customerId);
        item.Amount.Should().Be(24750m);
        item.InstallmentCount.Should().Be(3);
        item.MaskedCardNumber.Should().Be("************1111");
        item.Status.Should().Be(PaymentStatus.Successful);

        await _dashboardRepository.Received(1).GetPaymentsByDateRangeAsync(
            _from, _to, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }
}
