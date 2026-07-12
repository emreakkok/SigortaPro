using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Payments;
using SigortaPro.Application.Features.Payments.Queries.GetInstallmentOptions;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Payments;

public class GetInstallmentOptionsQueryHandlerTests
{
    private readonly IReadRepository<Quote> _quoteRepository = Substitute.For<IReadRepository<Quote>>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetInstallmentOptionsQueryHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;

    public GetInstallmentOptionsQueryHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);

        _handler = new GetInstallmentOptionsQueryHandler(
            _quoteRepository, _customerRepository, _paymentService, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ReturnMappedOptions_When_QuoteApprovedAndOwned()
    {
        var quote = PaymentTestData.ApprovedQuote(_customer.Id, 12000m, DateTime.UtcNow.AddDays(5));
        _quoteRepository.GetByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);
        _paymentService.GetInstallmentOptions(12000m).Returns(new List<InstallmentOption>
        {
            new(1, 12000m, 12000m),
            new(3, 4000m, 12000m),
        });

        var result = await _handler.Handle(new GetInstallmentOptionsQuery(quote.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Count.Should().Be(1);
        result[1].MonthlyAmount.Should().Be(4000m);
        _paymentService.Received(1).GetInstallmentOptions(12000m);
    }

    [Fact]
    public async Task Handle_Should_ThrowBusinessRule_When_QuoteNotApproved()
    {
        var quote = new Quote(_customer.Id, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(12000m, DateTime.UtcNow.AddDays(5)); // Priced, Approved değil.
        _quoteRepository.GetByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var act = () => _handler.Handle(new GetInstallmentOptionsQuery(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        _paymentService.DidNotReceive().GetInstallmentOptions(Arg.Any<decimal>());
    }
}
