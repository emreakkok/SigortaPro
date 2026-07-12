using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.DTOs;

// Branş bazlı poliçe/prim dağılımı kalemi.
public sealed record BranchDistributionPointDto(
    InsuranceBranch Branch,
    int PolicyCount,
    decimal PremiumTotal);
