using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Quotes.Commands.ApproveQuote;

public sealed class ApproveQuoteCommandHandler : ICommandHandler<ApproveQuoteCommand, QuoteSummaryDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveQuoteCommandHandler> _logger;

    public ApproveQuoteCommandHandler(
        IQuoteRepository quoteRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<ApproveQuoteCommandHandler> logger)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<QuoteSummaryDto> Handle(ApproveQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetTrackedByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quote), request.QuoteId);

        await QuoteAuthorization.EnsureCanAccessAsync(
            quote.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        // Geçerlilik süresi dolmuş bir teklif onaylanamaz (Expired'a çekme işi arkaplan servisidir).
        if (quote.ValidUntil is not null && _dateTimeProvider.UtcNow > quote.ValidUntil)
        {
            throw new BusinessRuleException("Teklifin geçerlilik süresi dolmuş, onaylanamaz.");
        }

        // Priced değilse DomainException → UnhandledExceptionBehavior 409'a çevirir.
        quote.Approve();

        _quoteRepository.Update(quote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Teklif onaylandı. QuoteId: {QuoteId}", quote.Id);

        return QuoteMappings.ToSummaryDto(quote);
    }
}
