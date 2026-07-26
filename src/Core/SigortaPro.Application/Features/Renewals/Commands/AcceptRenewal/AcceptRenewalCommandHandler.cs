using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Application.Features.Renewals.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

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

        // İDEMPOTENT ONAY: Yeni dönem teklifi başka bir yoldan da (teklif detay ekranı — POST /quotes/{id}/approve)
        // onaylanmış olabilir. Bu durumda tekrar Approve edilmez (aksi halde Priced şartı nedeniyle DomainException 409
        // oluşurdu); yenileme kaydı da yalnızca gerekiyorsa işaretlenir. Böylece müşteri hangi yoldan onaylarsa
        // onaylasın renewal ↔ quote durumu tutarlı kalır ve ödeme aşamasına ilerleyebilir.
        switch (newQuote.Status)
        {
            case QuoteStatus.Priced:
                // Geçerlilik süresi yalnızca henüz onaylanmamış (Priced) teklif için anlamlıdır.
                if (newQuote.ValidUntil is not null && now > newQuote.ValidUntil)
                {
                    throw new BusinessRuleException("Yenileme teklifinin geçerlilik süresi dolmuş, onaylanamaz.");
                }

                newQuote.Approve();
                _quoteRepository.Update(newQuote);
                break;

            case QuoteStatus.Approved:
                // Zaten ödeme aşamasına hazır — idempotent (tekrar onaylama yapılmaz).
                break;

            default:
                // Satın alınmış / reddedilmiş / süresi dolmuş teklif onaylanamaz.
                throw new BusinessRuleException("Yenileme teklifi bu durumda onaylanamaz.");
        }

        if (!renewal.IsAccepted)
        {
            renewal.Accept(now);
            _renewalRepository.Update(renewal);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Yenileme teklifi onaylandı. RenewalId: {RenewalId}, NewQuoteId: {QuoteId}, QuoteStatus: {Status}",
            renewal.Id, newQuote.Id, newQuote.Status);

        return RenewalMappings.ToDto(renewal);
    }
}
