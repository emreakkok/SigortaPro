using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.DTOs;

// Tarih aralıklı poliçe raporu kalemi (başlangıç tarihine göre filtrelenir).
public sealed record PolicyReportItemDto(
    Guid Id,
    string PolicyNumber,
    string CustomerFullName,
    InsuranceBranch Branch,
    PolicyStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPremium,
    // Müşteri kimliği (additive) — aynı isimli müşterileri ayırt etmek için telefon + stabil CustomerId.
    Guid CustomerId = default,
    string? CustomerPhone = null);
