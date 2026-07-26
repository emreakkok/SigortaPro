using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Claims.DTOs;

// Hasar detayı: durum, tutarlar ve değerlendirme notu ile birlikte. Poliçe numarası okunabilirlik için taşınır.
public sealed record ClaimDto(
    Guid Id,
    Guid PolicyId,
    string PolicyNumber,
    Guid CustomerId,
    DateTime IncidentDate,
    string Description,
    decimal EstimatedAmount,
    decimal? ApprovedAmount,
    ClaimStatus Status,
    string? ReviewNote,
    DateTime CreatedAt,
    // Müşterinin eklediği belgeler (foto/PDF). Hem müşteri hem Admin/Personel detayında döner.
    IReadOnlyList<ClaimDocumentDto> Documents);
