using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// Branş performansı — TEK kohort üzerinden: aralıkta oluşturulan teklifler (Quote.CreatedAt) ve bunların
// poliçeleşen kısmı. Tek sorgu/tek kaynak olduğundan dönüşüm oranı asla %100'ü aşamaz (dönem kayması yok).
// PremiumTotal = poliçeleşen tekliflerin prim toplamı. API DTO'su değil; salt okunur sorgu sonucudur (ADR-026).
public sealed record BranchPerformanceAggregate(
    InsuranceBranch Branch,
    int QuoteCount,
    int PurchasedCount,
    decimal PremiumTotal);
