using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Claims.DTOs;

// Hasar listesi görünümü için özet DTO (değerlendirme notu/açıklama içermez).
public sealed record ClaimSummaryDto(
    Guid Id,
    Guid PolicyId,
    string PolicyNumber,
    ClaimStatus Status,
    DateTime IncidentDate,
    decimal EstimatedAmount,
    decimal? ApprovedAmount,
    DateTime CreatedAt);
