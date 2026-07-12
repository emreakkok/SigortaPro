using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.Commands.ApproveQuote;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class ApproveQuoteCommandHandlerTests
{
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ApproveQuoteCommandHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;
    private readonly DateTime _now = new(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

    public ApproveQuoteCommandHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);
        _dateTimeProvider.UtcNow.Returns(_now);

        _handler = new ApproveQuoteCommandHandler(
            _quoteRepository, _customerRepository, _currentUserService, _dateTimeProvider, _unitOfWork,
            Substitute.For<ILogger<ApproveQuoteCommandHandler>>());
    }

    private Quote PricedQuote(DateTime validUntil)
    {
        var quote = new Quote(_customer.Id, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(20000m, validUntil);
        return quote;
    }

    [Fact]
    public async Task Handle_Should_ApproveQuote_When_PricedAndValid()
    {
        var quote = PricedQuote(_now.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var result = await _handler.Handle(new ApproveQuoteCommand(quote.Id), CancellationToken.None);

        quote.Status.Should().Be(QuoteStatus.Approved);
        result.Status.Should().Be(QuoteStatus.Approved);
        _quoteRepository.Received(1).Update(quote);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowBusinessRule_When_QuoteValidityExpired()
    {
        var quote = PricedQuote(_now.AddDays(-1));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var act = () => _handler.Handle(new ApproveQuoteCommand(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_QuoteBelongsToAnotherCustomer()
    {
        var otherCustomer = CustomerTestData.CreateCustomer(Guid.NewGuid(), Guid.NewGuid());
        var quote = new Quote(otherCustomer.Id, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(20000m, _now.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var act = () => _handler.Handle(new ApproveQuoteCommand(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
