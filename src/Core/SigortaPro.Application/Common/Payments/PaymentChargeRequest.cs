namespace SigortaPro.Application.Common.Payments;

// Mock sanal POS'a iletilen ödeme talebi. Ham kart verisi taşır; yalnızca bellekte kullanılır,
// asla log'a basılmaz veya kalıcılaştırılmaz. Kalıcı kayıt yalnızca maskeli kartla tutulur.
public sealed record PaymentChargeRequest(
    string CardNumber,
    string CardHolderName,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvv,
    decimal Amount,
    int InstallmentCount);
