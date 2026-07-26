using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Notifications;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Renewals.Commands.GeneratePolicyRenewals;

public sealed class GeneratePolicyRenewalsCommandHandler : ICommandHandler<GeneratePolicyRenewalsCommand, int>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IRenewalRepository _renewalRepository;
    private readonly IClaimRepository _claimRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingRateResolver _pricingRateResolver;
    private readonly IQuotePricingInputBuilder _pricingInputBuilder;
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GeneratePolicyRenewalsCommandHandler> _logger;

    public GeneratePolicyRenewalsCommandHandler(
        IPolicyRepository policyRepository,
        IQuoteRepository quoteRepository,
        IRenewalRepository renewalRepository,
        IClaimRepository claimRepository,
        IPricingEngine pricingEngine,
        IPricingRateResolver pricingRateResolver,
        IQuotePricingInputBuilder pricingInputBuilder,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<GeneratePolicyRenewalsCommandHandler> logger)
    {
        _policyRepository = policyRepository;
        _quoteRepository = quoteRepository;
        _renewalRepository = renewalRepository;
        _claimRepository = claimRepository;
        _pricingEngine = pricingEngine;
        _pricingRateResolver = pricingRateResolver;
        _pricingInputBuilder = pricingInputBuilder;
        _notificationService = notificationService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(GeneratePolicyRenewalsCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var windowEnd = now.AddDays(BusinessConstants.RenewalNoticeWindowDays);

        var duePolicies = await _policyRepository.GetDueForRenewalAsync(now, windowEnd, cancellationToken);
        if (duePolicies.Count == 0)
        {
            return 0;
        }

        // ADR-048: yenileme teklifleri yeni dönem için üretildiğinden güncel tarifeyle fiyatlanır
        // (tüm parti için bir kez çözülür); kaynak poliçe/teklif fiyatlarına dokunulmaz.
        var effectivePricing = await _pricingRateResolver.ResolveEffectiveAsync(now, cancellationToken);

        var notifications = new List<RenewalOfferedNotification>();

        foreach (var policy in duePolicies)
        {
            var originalQuote = policy.Quote;
            var customer = policy.Customer;
            var product = originalQuote?.InsuranceProduct;
            if (originalQuote is null || customer is null || product is null)
            {
                // Beklenmez (repository ilişkileri yükler); veri tutarsızlığında bu poliçe atlanır.
                _logger.LogWarning("Yenileme atlandı: poliçe {PolicyId} teklif/ürün/müşteri verisi eksik.", policy.Id);
                continue;
            }

            // ADR-056/058/059: Girdi (Bonus-Malus basamağı dahil) ORTAK builder'dan kurulur → yenileme,
            // teklif oluşturma ve önizleme aynı fiyatlama girdisini üretir. Hasar geçmişi artık ayrı bir
            // ClaimHistoryFactor ile değil, bu basamakla fiyatlanır (ADR-059).
            // Sigara beyanı kaynak teklifin beyanından taşınır (arkaplan işinde müşteriye soru sorulamaz).
            var snapshot = await _pricingInputBuilder.BuildAsync(
                originalQuote.Branch, customer, originalQuote.Vehicle, originalQuote.Property, now,
                insuredBirthDate: originalQuote.InsuredPerson?.BirthDate,
                isSmoker: originalQuote.PricingSnapshot?.IsSmoker,
                cancellationToken: cancellationToken);

            // policy.EndDate: yenileme teklifi mevcut poliçe bitene kadar geçerli olmalı (aksi halde poliçe
            // aktifken teklif "süresi doldu" görünürdü — bkz. RenewalQuoteFactory geçerlilik hesabı).
            var renewalQuote = RenewalQuoteFactory.Build(
                originalQuote, customer, product, originalQuote.Vehicle, originalQuote.Property,
                _pricingEngine, now, policy.EndDate, snapshot, effectivePricing);

            await _quoteRepository.AddAsync(renewalQuote, cancellationToken);

            var renewal = new Renewal(policy.Id, renewalQuote.Id, now);
            await _renewalRepository.AddAsync(renewal, cancellationToken);

            notifications.Add(new RenewalOfferedNotification(
                customer.Id, policy.PolicyNumber, renewalQuote.Branch,
                renewalQuote.TotalPremium, renewalQuote.ValidUntil!.Value));
        }

        if (notifications.Count == 0)
        {
            return 0;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Bildirimler yalnızca kalıcılaştırılan teklifler için gönderilir (mock — log/e-posta simülasyonu).
        foreach (var notification in notifications)
        {
            await _notificationService.NotifyRenewalOfferedAsync(notification, cancellationToken);
        }

        _logger.LogInformation("Arkaplan: {Count} poliçe için yenileme teklifi üretildi.", notifications.Count);

        return notifications.Count;
    }
}
