using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Payments;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
internal static class PaymentMappings
{
    public static PurchaseResultDto ToPurchaseResult(Payment payment, Policy policy) => new(
        payment.Id,
        payment.Status,
        payment.MaskedCardNumber,
        payment.Amount,
        payment.InstallmentCount,
        payment.ProviderReferenceCode,
        ToPolicySummary(policy));

    public static PolicySummaryDto ToPolicySummary(Policy policy) => new(
        policy.Id,
        policy.PolicyNumber,
        policy.Status,
        policy.StartDate,
        policy.EndDate,
        policy.TotalPremium);

    public static PaymentHistoryItemDto ToHistoryItem(Payment payment) => new(
        payment.Id,
        payment.QuoteId,
        payment.Amount,
        payment.InstallmentCount,
        payment.MaskedCardNumber,
        payment.Status,
        payment.TransactionDate,
        payment.FailureReason);
}
