using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Application.Features.Renewals.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Renewals.Commands.AcceptRenewal;

public sealed class AcceptRenewalCommandHandler : ICommandHandler<AcceptRenewalCommand, RenewalDto>
{
    private readonly IRenewalRepository _renewalRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptRenewalCommandHandler> _logger;

    public AcceptRenewalCommandHandler(
        IRenewalRepository renewalRepository,
        IQuoteRepository quoteRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<AcceptRenewalCommandHandler> logger)
    {
        _renewalRepository = renewalRepository;
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RenewalDto> Handle(AcceptRenewalCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var renewal = await _renewalRepository.GetTrackedByIdAsync(request.RenewalId, cancellationToken)
            ?? throw new NotFoundException(nameof(Renewal), request.RenewalId);

        var newQuote = renewal.NewQuote
            ?? throw new NotFoundException(nameof(Quote), renewal.NewQuoteId);

        // Kaynak sahipliği: müşteri yalnızca kendi yenileme teklifini onaylar (QuoteAuthorization — Task 9).
        await QuoteAuthorization.EnsureCanAccessAsync(
            newQuote.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        if (newQuote.ValidUntil is not null && now > newQuote.ValidUntil)
        {
            throw new BusinessRuleException("Yenileme teklifinin geçerlilik süresi dolmuş, onaylanamaz.");
        }

        // Zaten onaylanmış → DomainException 409; teklif Priced değilse Approve() DomainException 409 (ADR-013).
        renewal.Accept(now);
        newQuote.Approve();

        _renewalRepository.Update(renewal);
        _quoteRepository.Update(newQuote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Yenileme teklifi onaylandı. RenewalId: {RenewalId}, NewQuoteId: {QuoteId}",
            renewal.Id, newQuote.Id);

        return RenewalMappings.ToDto(renewal);
    }
}
