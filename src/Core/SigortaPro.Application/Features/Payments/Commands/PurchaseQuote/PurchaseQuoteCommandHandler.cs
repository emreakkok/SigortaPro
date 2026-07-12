using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Payments;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;

public sealed class PurchaseQuoteCommandHandler : ICommandHandler<PurchaseQuoteCommand, PurchaseResultDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IPaymentService _paymentService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PurchaseQuoteCommandHandler> _logger;

    public PurchaseQuoteCommandHandler(
        ICustomerRepository customerRepository,
        IQuoteRepository quoteRepository,
        IPaymentRepository paymentRepository,
        IPolicyRepository policyRepository,
        IPaymentService paymentService,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<PurchaseQuoteCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _quoteRepository = quoteRepository;
        _paymentRepository = paymentRepository;
        _policyRepository = policyRepository;
        _paymentService = paymentService;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PurchaseResultDto> Handle(PurchaseQuoteCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var quote = await _quoteRepository.GetTrackedByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quote), request.QuoteId);

        await QuoteAuthorization.EnsureCanAccessAsync(
            quote.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        // Kartı çekmeden önce iş kurallarını doğrula: yalnızca onaylanmış ve geçerlilik süresi dolmamış teklif ödenir.
        if (quote.Status != QuoteStatus.Approved)
        {
            throw new BusinessRuleException("Yalnızca onaylanmış teklif satın alınabilir.");
        }

        if (quote.ValidUntil is not null && now > quote.ValidUntil)
        {
            throw new BusinessRuleException("Teklifin geçerlilik süresi dolmuş, satın alınamaz.");
        }

        var chargeRequest = new PaymentChargeRequest(
            request.CardNumber,
            request.CardHolderName,
            request.ExpiryMonth,
            request.ExpiryYear,
            request.Cvv,
            quote.TotalPremium,
            request.InstallmentCount);

        var gatewayResult = await _paymentService.ChargeAsync(chargeRequest, cancellationToken);

        // Maskeli kart numarası gateway'den gelir (ham kartı yalnızca gateway görür); Payment daima maskeli kaydeder.
        var payment = new Payment(
            customer.Id, quote.Id, quote.TotalPremium, request.InstallmentCount,
            gatewayResult.MaskedCardNumber, now);

        if (!gatewayResult.IsSuccess)
        {
            // Başarısız deneme de kayıt altına alınır (ödeme geçmişinde görünür), ardından 402 fırlatılır.
            payment.MarkFailed(gatewayResult.FailureReason ?? "Ödeme reddedildi.");
            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Ödeme başarısız. QuoteId: {QuoteId}, PaymentId: {PaymentId}, Sebep: {Reason}",
                quote.Id, payment.Id, payment.FailureReason);

            throw new PaymentFailedException(payment.FailureReason!);
        }

        // Başarılı ödeme + poliçe üretimi + teklif durumu tek transaction'da atomik yürütülür (ADR-017, ADR-022).
        Policy policy = null!;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            payment.MarkSuccessful(gatewayResult.ProviderReferenceCode);

            // Approved değilse DomainException → UnhandledExceptionBehavior 409'a çevirir (ADR-013).
            quote.Purchase();

            var sequence = await _policyRepository.CountByYearAsync(now.Year, cancellationToken) + 1;
            var policyNumber = PolicyNumberFactory.Format(now.Year, sequence);
            policy = new Policy(
                policyNumber, customer.Id, quote.Id,
                now, now.AddYears(BusinessConstants.PolicyTermYears), quote.TotalPremium);

            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _policyRepository.AddAsync(policy, cancellationToken);
            _quoteRepository.Update(quote);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        _logger.LogInformation(
            "Ödeme başarılı ve poliçe oluşturuldu. QuoteId: {QuoteId}, PolicyId: {PolicyId}, PoliçeNo: {PolicyNumber}",
            quote.Id, policy.Id, policy.PolicyNumber);

        return PaymentMappings.ToPurchaseResult(payment, policy);
    }
}
