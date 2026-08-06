using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Common.Notifications;
using SigortaPro.Application.Features.Auth.Commands.ForgotPassword;
using SigortaPro.Application.Features.Auth.Commands.Register;
using SigortaPro.Application.Features.Claims.Commands.ApproveClaim;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Features.Claims.Commands.PayClaim;
using SigortaPro.Application.Features.Claims.Commands.RejectClaim;
using SigortaPro.Application.Features.Claims.Commands.StartClaimReview;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;

namespace SigortaPro.Application.Common.Behaviors;

// Gerçek zamanlı bildirimler, iş handler'larına dokunmadan tek bir cross-cutting pipeline
// behavior'ından yayınlanır. Handler başarıyla tamamlandıktan
// sonra komut türü bildirim kataloğuna eşlenir; eşleşme yoksa hiçbir şey yapılmaz. Böylece:
//  - CQRS handler'ları bildirim altyapısını bilmez (SRP; mevcut handler/test'ler değişmez),
//  - olay→bildirim eşlemesi tek yerde genişletilir (yeni olay = bu katalogda bir blok).
// Katalog artık "kim yaptı / kimin için / hangi kayıt" bağlamını üretir (INotificationContextResolver).
// Bağlam çözümü ve yayın hatası iş akışını ASLA bozamaz: tamamı geniş yakalanıp loglanır
// (yutulmaz — loglanır; bildirim yan kanaldır, iş sonucu değildir).
public sealed class RealTimeNotificationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly INotificationContextResolver _contextResolver;
    private readonly ILogger<RealTimeNotificationBehavior<TRequest, TResponse>> _logger;

    public RealTimeNotificationBehavior(
        INotificationDispatcher notificationDispatcher,
        INotificationContextResolver contextResolver,
        ILogger<RealTimeNotificationBehavior<TRequest, TResponse>> logger)
    {
        _notificationDispatcher = notificationDispatcher;
        _contextResolver = contextResolver;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        try
        {
            // Bağlam çözümü de (DB okuması) yan kanaldır — bu yüzden yayınla aynı korumanın içindedir.
            var notification = await MapToNotificationAsync(request, response, cancellationToken);
            if (notification is not null)
            {
                await _notificationDispatcher.PublishToStaffAsync(notification, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Bildirim bir yan kanaldır; hata tamamlanmış iş operasyonunu geri alamaz/bozamaz.
            _logger.LogError(exception, "Gerçek zamanlı bildirim üretilemedi/yayınlanamadı. İstek: {RequestType}", typeof(TRequest).Name);
        }

        return response;
    }

    // Olay → bildirim kataloğu.
    // KVKK/veri minimizasyonu: TCKN, tam telefon, kart/CVV, şifre/token ve sağlık detayı ASLA yazılmaz.
    // Yalnızca operasyon için gereken ad, ürün adı, tutar ve poliçe numarası taşınır.
    private async Task<RealTimeNotification?> MapToNotificationAsync(
        TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        switch (request)
        {
            // Kayıt anonim akıştır; ad-soyad komuttan gelir (ek sorgu yok). Actor = kaydolan kişinin kendisi.
            case RegisterCommand register when IsSuccess(response):
            {
                var fullName = $"{register.FirstName} {register.LastName}".Trim();
                return new RealTimeNotification(
                    "customer-registered",
                    NotificationSeverity.Success,
                    "Yeni müşteri kaydı",
                    $"{fullName} sisteme müşteri olarak kaydoldu.",
                    RelatedEntityType: "Customer",
                    ActorName: fullName);
            }

            case CreateQuoteCommand when response is QuoteDto quote:
            {
                var actor = await _contextResolver.ResolveActorAsync(cancellationToken);
                var customerName = await _contextResolver.GetCustomerNameAsync(quote.CustomerId, cancellationToken)
                                   ?? "Müşteri";
                var actorName = actor.DisplayName ?? customerName;
                var premium = FormatCurrency(quote.TotalPremium);

                // Sağlıkta "başkası adına": yalnızca sigortalının adı — TCKN/sağlık detayı yazılmaz.
                var insuredSuffix = quote.InsuredPerson is null
                    ? string.Empty
                    : $" Sigortalı: {quote.InsuredPerson.FullName}.";

                var message = actor.IsStaff
                    ? $"{actorName} tarafından {customerName} adına {quote.ProductName} teklifi oluşturuldu. Prim: {premium}.{insuredSuffix}"
                    : $"{customerName}, {quote.ProductName} teklifi oluşturdu. Prim: {premium}.{insuredSuffix}";

                return new RealTimeNotification(
                    "quote-created",
                    NotificationSeverity.Info,
                    "Yeni teklif oluşturuldu",
                    message,
                    quote.Id,
                    "Quote",
                    actor.UserId,
                    actorName);
            }

            // Satın alma müşterinin kendi teklifi üzerinden yapılır → actor = satın alan müşteri.
            case PurchaseQuoteCommand when response is PurchaseResultDto purchase:
            {
                var actor = await _contextResolver.ResolveActorAsync(cancellationToken);
                var actorName = actor.DisplayName ?? "Müşteri";
                var premium = FormatCurrency(purchase.Policy.TotalPremium);

                return new RealTimeNotification(
                    "policy-created",
                    NotificationSeverity.Success,
                    "Yeni poliçe satışı",
                    $"{actorName}, {purchase.Policy.PolicyNumber} numaralı poliçeyi satın aldı. Prim: {premium}.",
                    purchase.Policy.Id,
                    "Policy",
                    actor.UserId,
                    actorName,
                    purchase.Policy.PolicyNumber);
            }

            // hasar bildiriminde RelatedEntityId artık set edilir (önceden navigasyon yapılamıyordu).
            case CreateClaimCommand when response is ClaimDto claim:
            {
                var actor = await _contextResolver.ResolveActorAsync(cancellationToken);
                var customerName = await _contextResolver.GetCustomerNameAsync(claim.CustomerId, cancellationToken)
                                   ?? "Müşteri";
                var actorName = actor.DisplayName ?? customerName;
                var estimated = FormatCurrency(claim.EstimatedAmount);

                return new RealTimeNotification(
                    "claim-created",
                    NotificationSeverity.Warning,
                    "Yeni hasar bildirimi",
                    $"{customerName}, {claim.PolicyNumber} numaralı poliçe için hasar bildirdi. Tahmini tutar: {estimated}.",
                    claim.Id,
                    "Claim",
                    actor.UserId,
                    actorName,
                    claim.PolicyNumber);
            }

            case StartClaimReviewCommand when response is ClaimSummaryDto summary:
                return await ClaimStatusChangedAsync(summary, "incelemeye alındı", NotificationSeverity.Info, cancellationToken);

            case ApproveClaimCommand when response is ClaimSummaryDto summary:
                return await ClaimStatusChangedAsync(
                    summary,
                    summary.ApprovedAmount is null
                        ? "onaylandı"
                        : $"onaylandı (onaylanan tutar: {FormatCurrency(summary.ApprovedAmount.Value)})",
                    NotificationSeverity.Success,
                    cancellationToken);

            case RejectClaimCommand when response is ClaimSummaryDto summary:
                return await ClaimStatusChangedAsync(summary, "reddedildi", NotificationSeverity.Warning, cancellationToken);

            case PayClaimCommand when response is ClaimSummaryDto summary:
                return await ClaimStatusChangedAsync(summary, "ödemesi gerçekleştirildi", NotificationSeverity.Success, cancellationToken);

            // Anonim akış: hesap varlığını sızdırmamak için e-posta/kimlik bilgisi YAZILMAZ.
            case ForgotPasswordCommand:
                return new RealTimeNotification(
                    "password-reset-requested",
                    NotificationSeverity.Info,
                    "Şifre sıfırlama isteği",
                    "Bir hesap için şifre sıfırlama talebi alındı.");

            default:
                return null;
        }
    }

    // Hasar durum geçişleri personel tarafından yapılır → "Güncelleyen" bilgisi actor'dan gelir.
    private async Task<RealTimeNotification> ClaimStatusChangedAsync(
        ClaimSummaryDto summary, string statusPhrase, string severity, CancellationToken cancellationToken)
    {
        var actor = await _contextResolver.ResolveActorAsync(cancellationToken);

        return new RealTimeNotification(
            "claim-status-changed",
            severity,
            "Hasar durumu güncellendi",
            $"{summary.PolicyNumber} numaralı poliçenin hasar dosyası {statusPhrase}.",
            summary.Id,
            "Claim",
            actor.UserId,
            actor.DisplayName,
            summary.PolicyNumber);
    }

    private static string FormatCurrency(decimal amount) =>
        string.Format(TurkishCulture, "{0:N2} ₺", amount);

    // Result dönen komutlarda (Register) yalnızca başarıda bildirim üretilir;
    // soft-fail (duplicate TCKN vb.) bildirim tetiklemez.
    private static bool IsSuccess(TResponse response) => response is Result { IsSuccess: true };
}
