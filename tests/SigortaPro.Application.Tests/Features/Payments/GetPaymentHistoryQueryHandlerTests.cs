using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Payments.Queries.GetPaymentHistory;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Payments;

public class GetPaymentHistoryQueryHandlerTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetPaymentHistoryQueryHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;

    public GetPaymentHistoryQueryHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);

        _handler = new GetPaymentHistoryQueryHandler(_paymentRepository, _customerRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ReturnOwnPaymentsMappedToDto_When_CustomerHasHistory()
    {
        var quoteId = Guid.NewGuid();
        var payment = new Payment(_customer.Id, quoteId, 20000m, 3, "************1111", DateTime.UtcNow);
        payment.MarkSuccessful("POS-REF");

        _paymentRepository.GetByCustomerAsync(_customer.Id, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Payment>(new[] { payment }, 1, 20, 1));

        var result = await _handler.Handle(new GetPaymentHistoryQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        var item = result.Items.Single();
        item.QuoteId.Should().Be(quoteId);
        item.MaskedCardNumber.Should().Be("************1111");
        item.InstallmentCount.Should().Be(3);

        // Sahiplik: yalnızca oturum sahibinin müşteri Id'siyle sorgulanır.
        await _paymentRepository.Received(1).GetByCustomerAsync(
            _customer.Id, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }
}
