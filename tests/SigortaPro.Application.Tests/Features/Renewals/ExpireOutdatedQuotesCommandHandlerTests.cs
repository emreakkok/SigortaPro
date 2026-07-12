using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.Commands.ExpireOutdatedQuotes;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Renewals;

public class ExpireOutdatedQuotesCommandHandlerTests
{
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ExpireOutdatedQuotesCommandHandler _handler;

    private readonly DateTime _now = new(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);

    public ExpireOutdatedQuotesCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        _handler = new ExpireOutdatedQuotesCommandHandler(
            _quoteRepository, _dateTimeProvider, _unitOfWork,
            Substitute.For<ILogger<ExpireOutdatedQuotesCommandHandler>>());
    }

    private Quote OutdatedPricedQuote()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(20000m, _now.AddDays(-1)); // geçerlilik süresi dolmuş
        return quote;
    }

    [Fact]
    public async Task Handle_Should_ExpireQuotesAndReturnCount_When_OutdatedQuotesExist()
    {
        var quotes = new[] { OutdatedPricedQuote(), OutdatedPricedQuote() };
        _quoteRepository.GetExpirableAsync(_now, Arg.Any<CancellationToken>()).Returns(quotes);

        var count = await _handler.Handle(new ExpireOutdatedQuotesCommand(), CancellationToken.None);

        count.Should().Be(2);
        quotes.Should().OnlyContain(quote => quote.Status == QuoteStatus.Expired);
        _quoteRepository.Received(2).Update(Arg.Any<Quote>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnZeroAndNotSave_When_NoOutdatedQuotes()
    {
        _quoteRepository.GetExpirableAsync(_now, Arg.Any<CancellationToken>()).Returns(Array.Empty<Quote>());

        var count = await _handler.Handle(new ExpireOutdatedQuotesCommand(), CancellationToken.None);

        count.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
