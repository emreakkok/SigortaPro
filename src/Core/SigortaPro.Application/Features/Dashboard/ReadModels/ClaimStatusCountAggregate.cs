using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// Bir tarih aralığında BİLDİRİLEN hasarların (Claim.CreatedAt) durum kırılımı.
// ApprovedTotal yalnızca onay tutarı GİRİLMİŞ kayıtlardan gelir (Approved/Paid) — tahmini tutarla karıştırılmaz.
// API DTO'su değil; salt okunur sorgu sonucudur (ADR-026).
public sealed record ClaimStatusCountAggregate(
    ClaimStatus Status,
    int Count,
    decimal EstimatedTotal,
    decimal ApprovedTotal);
