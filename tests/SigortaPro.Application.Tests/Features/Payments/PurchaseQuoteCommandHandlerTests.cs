using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Payments;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Payments;

public class PurchaseQuoteCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PurchaseQuoteCommandHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;
    private readonly DateTime _now = new(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

    public PurchaseQuoteCommandHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);
        _dateTimeProvider.UtcNow.Returns(_now);
        _policyRepository.CountByYearAsync(_now.Year, Arg.Any<CancellationToken>()).Returns(0);

        // ExecuteInTransactionAsync verilen operasyonu gerçekten çalıştırsın.
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>().Invoke());

        _handler = new PurchaseQuoteCommandHandler(
            _customerRepository, _quoteRepository, _paymentRepository, _policyRepository,
            _paymentService, _dateTimeProvider, _currentUserService, _unitOfWork,
            Substitute.For<ILogger<PurchaseQuoteCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_CreatePolicyAndPurchaseQuote_When_PaymentSucceeds()
    {
        var quote = PaymentTestData.ApprovedQuote(_customer.Id, 20000m, _now.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);
        _paymentService.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentGatewayResult.Success("************1111", "POS-REF-01"));

        var result = await _handler.Handle(PaymentTestData.PurchaseCommand(quote.Id), CancellationToken.None);

        result.PaymentStatus.Should().Be(PaymentStatus.Successful);
        result.MaskedCardNumber.Should().Be("************1111");
        result.Amount.Should().Be(20000m);
        result.ProviderReferenceCode.Should().Be("POS-REF-01");

        result.Policy.PolicyNumber.Should().Be("POL-2026-000001");
        result.Policy.Status.Should().Be(PolicyStatus.Active);
        result.Policy.StartDate.Should().Be(_now);
        result.Policy.EndDate.Should().Be(_now.AddYears(1));
        result.Policy.TotalPremium.Should().Be(20000m);

        quote.Status.Should().Be(QuoteStatus.Purchased);

        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Payment>(p => p.Status == PaymentStatus.Successful), Arg.Any<CancellationToken>());
        await _policyRepository.Received(1).AddAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>());
        _quoteRepository.Received(1).Update(quote);
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FormatSequentialPolicyNumber_When_PriorPoliciesExistInYear()
    {
        var quote = PaymentTestData.ApprovedQuote(_customer.Id, 20000m, _now.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);
        _policyRepository.CountByYearAsync(_now.Year, Arg.Any<CancellationToken>()).Returns(41);
        _paymentService.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentGatewayResult.Success("************1111", "POS-REF-42"));

        var result = await _handler.Handle(PaymentTestData.PurchaseCommand(quote.Id), CancellationToken.None);

        result.Policy.PolicyNumber.Should().Be("POL-2026-000042");
    }

    [Fact]
    public async Task Handle_Should_RecordFailedPaymentAndThrow_When_GatewayDeclines()
    {
        var quote = PaymentTestData.ApprovedQuote(_customer.Id, 20000m, _now.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);
        _paymentService.ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentGatewayResult.Failure("************0002", "Yetersiz bakiye."));

        var act = () => _handler.Handle(PaymentTestData.PurchaseCommand(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentFailedException>().WithMessage("Yetersiz bakiye.");

        // Başarısız deneme kaydedilir; poliçe/teklif değişmez.
        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<Payment>(p => p.Status == PaymentStatus.Failed), Arg.Any<CancellationToken>());
        await _policyRepository.DidNotReceive().AddAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>());
        quote.Status.Should().Be(QuoteStatus.Approved);
    }

    [Fact]
    public async Task Handle_Should_NotChargeCard_When_QuoteNotApproved()
    {
        var quote = new Quote(_customer.Id, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(20000m, _now.AddDays(5)); // Priced, henüz Approved değil.
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var act = () => _handler.Handle(PaymentTestData.PurchaseCommand(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _paymentService.DidNotReceive().ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotChargeCard_When_QuoteValidityExpired()
    {
        var quote = PaymentTestData.ApprovedQuote(_customer.Id, 20000m, _now.AddDays(-1));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var act = () => _handler.Handle(PaymentTestData.PurchaseCommand(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _paymentService.DidNotReceive().ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_QuoteBelongsToAnotherCustomer()
    {
        var quote = PaymentTestData.ApprovedQuote(Guid.NewGuid(), 20000m, _now.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var act = () => _handler.Handle(PaymentTestData.PurchaseCommand(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _paymentService.DidNotReceive().ChargeAsync(Arg.Any<PaymentChargeRequest>(), Arg.Any<CancellationToken>());
    }
}
