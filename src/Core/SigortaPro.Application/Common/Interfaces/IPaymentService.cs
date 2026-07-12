using SigortaPro.Application.Common.Payments;

namespace SigortaPro.Application.Common.Interfaces;

// Mock sanal POS soyutlaması (ADR-007). Implementasyonu Infrastructure'da (MockVirtualPosService).
// İleride gerçek POS'a geçiş yalnızca implementasyon değişikliğidir.
public interface IPaymentService
{
    // Kartı doğrular (Luhn) ve senaryo bazlı sonuç üretir. Kart verisi log'a basılmaz; sonuç maskeli kartla döner.
    Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);

    // Verilen tutar için taksit seçeneklerini üretir (faizsiz mock).
    IReadOnlyList<InstallmentOption> GetInstallmentOptions(decimal amount);
}
