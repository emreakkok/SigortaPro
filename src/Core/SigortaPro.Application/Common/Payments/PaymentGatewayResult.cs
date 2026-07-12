namespace SigortaPro.Application.Common.Payments;

// Mock sanal POS sonucu. Kart maskeleme gateway'in sorumluluğundadır (ham kartı yalnızca gateway görür);
// döndürülen MaskedCardNumber doğrudan Payment kaydına yazılır.
public sealed record PaymentGatewayResult(
    bool IsSuccess,
    string MaskedCardNumber,
    string? ProviderReferenceCode,
    string? FailureReason)
{
    public static PaymentGatewayResult Success(string maskedCardNumber, string providerReferenceCode) =>
        new(true, maskedCardNumber, providerReferenceCode, null);

    public static PaymentGatewayResult Failure(string maskedCardNumber, string failureReason) =>
        new(false, maskedCardNumber, null, failureReason);
}
