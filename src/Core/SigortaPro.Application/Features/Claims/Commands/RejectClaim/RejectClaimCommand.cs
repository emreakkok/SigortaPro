using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Commands.RejectClaim;

// Personel incelemedeki hasarı reddeder (UnderReview → Rejected); gerekçe değerlendirme notu olarak zorunludur.
// ClaimId route'tan set edilir.
public sealed record RejectClaimCommand(
    Guid ClaimId,
    string ReviewNote) : ICommand<ClaimSummaryDto>;
