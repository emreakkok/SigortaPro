using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz).
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
        claim.CreatedAt,
        claim.Documents
            .OrderBy(document => document.CreatedAt)
            .Select(ToDocumentDto)
            .ToList());

    public static ClaimDocumentDto ToDocumentDto(ClaimDocument document) => new(
        document.Id,
        document.FileName,
        document.ContentType,
        document.FileSizeBytes,
        ClaimDocumentStorage.IsImage(document.ContentType),
        document.CreatedAt);

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
