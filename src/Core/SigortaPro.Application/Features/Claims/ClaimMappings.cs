using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
internal static class ClaimMappings
{
    // Poliçe numarası, detay sorgusunda navigation'dan; oluşturma akışında ise yüklenen poliçeden gelir.
    public static ClaimDto ToDto(Claim claim, string policyNumber) => new(
        claim.Id,
        claim.PolicyId,
        policyNumber,
        claim.CustomerId,
        claim.IncidentDate,
        claim.Description,
        claim.EstimatedAmount,
        claim.ApprovedAmount,
        claim.Status,
        claim.ReviewNote,
        claim.CreatedAt);

    public static ClaimSummaryDto ToSummaryDto(Claim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.Policy?.PolicyNumber ?? string.Empty,
        claim.Status,
        claim.IncidentDate,
        claim.EstimatedAmount,
        claim.ApprovedAmount,
        claim.CreatedAt);
}
