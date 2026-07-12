using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Payments.DTOs;

namespace SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;

// Onaylanmış teklifi mock sanal POS ile satın alır. Ödeme başarılıysa poliçe üretilir ve teklif Purchased olur.
// Kart bilgileri yalnızca ödeme gateway'ine iletilir; kalıcı kayıt maskeli tutulur (ADR-007).
public sealed record PurchaseQuoteCommand(
    Guid QuoteId,
    string CardNumber,
    string CardHolderName,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvv,
    int InstallmentCount) : ICommand<PurchaseResultDto>;
