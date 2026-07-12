namespace SigortaPro.Application.Features.Dashboard.DTOs;

// En riskli müşteri segmenti kalemi: hasar sayısı ve fiyatlamaya etki eden hasar tutarı toplamıyla sıralanır.
public sealed record CustomerRiskSegmentDto(
    Guid CustomerId,
    string FullName,
    int ClaimCount,
    decimal TotalClaimAmount);
