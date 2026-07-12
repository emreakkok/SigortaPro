using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.Commands.RejectQuote;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class RejectQuoteCommandHandlerTests
{
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RejectQuoteCommandHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;

    public RejectQuoteCommandHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);

        _handler = new RejectQuoteCommandHandler(
            _quoteRepository, _customerRepository, _currentUserService, _unitOfWork,
            Substitute.For<ILogger<RejectQuoteCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_RejectQuote_When_OwnedAndRejectable()
    {
        var quote = new Quote(_customer.Id, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(20000m, DateTime.UtcNow.AddDays(5));
        _quoteRepository.GetTrackedByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        var result = await _handler.Handle(new RejectQuoteCommand(quote.Id), CancellationToken.None);

        quote.Status.Should().Be(QuoteStatus.Rejected);
        result.Status.Should().Be(QuoteStatus.Rejected);
        _quoteRepository.Received(1).Update(quote);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
