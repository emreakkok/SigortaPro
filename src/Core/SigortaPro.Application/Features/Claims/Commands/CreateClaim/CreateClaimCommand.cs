using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Commands.CreateClaim;

// Müşteri, aktif bir poliçesi için hasar bildirir (olay tarihi/açıklaması, tahmini tutar).
// PhotoFileNames mock foto yükleme metadatasıdır; gerçek foto depolama MVP dışıdır (PROJECT_CONTEXT §9, ADR-024)
// ve kalıcılaştırılmaz — arayüz ileride gerçek depolamaya hazır olacak şekilde tutulur.
public sealed record CreateClaimCommand(
    Guid PolicyId,
    DateTime IncidentDate,
    string Description,
    decimal EstimatedAmount,
    IReadOnlyList<string>? PhotoFileNames = null) : ICommand<ClaimDto>;
