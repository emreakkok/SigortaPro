using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Payments.DTOs;

// Müşteri ödeme geçmişi kalemi. Kart yalnızca maskeli taşınır; başarısız denemede sebep gösterilir.
public sealed record PaymentHistoryItemDto(
    Guid Id,
    Guid QuoteId,
    decimal Amount,
    int InstallmentCount,
    string MaskedCardNumber,
    PaymentStatus Status,
    DateTime TransactionDate,
    string? FailureReason);
