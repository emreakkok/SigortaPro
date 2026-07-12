using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.DTOs;

// Tarih aralıklı ödeme raporu kalemi (işlem tarihine göre filtrelenir). Kart yalnızca maskeli döner (CLAUDE.md §4.5).
public sealed record PaymentReportItemDto(
    Guid Id,
    Guid CustomerId,
    string CustomerFullName,
    decimal Amount,
    int InstallmentCount,
    string MaskedCardNumber,
    PaymentStatus Status,
    DateTime TransactionDate);
