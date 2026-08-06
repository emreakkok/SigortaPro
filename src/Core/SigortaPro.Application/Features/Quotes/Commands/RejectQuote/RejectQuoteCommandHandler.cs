using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Quotes.Commands.RejectQuote;

public sealed class RejectQuoteCommandHandler : ICommandHandler<RejectQuoteCommand, QuoteSummaryDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectQuoteCommandHandler> _logger;

    public RejectQuoteCommandHandler(
        IQuoteRepository quoteRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<RejectQuoteCommandHandler> logger)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<QuoteSummaryDto> Handle(RejectQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetTrackedByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quote), request.QuoteId);

        await QuoteAuthorization.EnsureCanAccessAsync(
            quote.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        // Satın alınmış/süresi dolmuş teklif reddedilemez → DomainException → 409.
        quote.Reject();

        _quoteRepository.Update(quote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Teklif reddedildi. QuoteId: {QuoteId}", quote.Id);

        return QuoteMappings.ToSummaryDto(quote);
    }
}
