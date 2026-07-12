using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Commands.ApproveClaim;

// Personel incelemedeki hasarı onaylar (UnderReview → Approved) ve onaylanan tutarı + değerlendirme notunu kaydeder.
// ClaimId route'tan set edilir; gövde onay tutarı ve notu taşır.
public sealed record ApproveClaimCommand(
    Guid ClaimId,
    decimal ApprovedAmount,
    string? ReviewNote) : ICommand<ClaimSummaryDto>;
