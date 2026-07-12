using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Quotes.Commands.ExpireOutdatedQuotes;

public sealed class ExpireOutdatedQuotesCommandHandler : ICommandHandler<ExpireOutdatedQuotesCommand, int>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpireOutdatedQuotesCommandHandler> _logger;

    public ExpireOutdatedQuotesCommandHandler(
        IQuoteRepository quoteRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<ExpireOutdatedQuotesCommandHandler> logger)
    {
        _quoteRepository = quoteRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(ExpireOutdatedQuotesCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var quotes = await _quoteRepository.GetExpirableAsync(now, cancellationToken);
        if (quotes.Count == 0)
        {
            return 0;
        }

        foreach (var quote in quotes)
        {
            // Repository yalnızca süresi dolmuş ve sonlanmamış teklifleri döndürür; Expire guard'ı ihlal etmez.
            quote.Expire(now);
            _quoteRepository.Update(quote);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Arkaplan: {Count} teklif geçerlilik süresi dolduğu için Expired'a çekildi.", quotes.Count);

        return quotes.Count;
    }
}
