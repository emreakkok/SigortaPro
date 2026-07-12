namespace SigortaPro.Application.Features.Dashboard.DTOs;

// Admin panelinin özet metrik kaynağı. Oranlar 0..1 aralığında ondalık olarak döner (frontend biçimlendirir):
// RenewalRate = onaylanan yenileme / sunulan yenileme; ClaimToPremiumRatio = ödenen hasar tutarı / üretilen prim.
public sealed record DashboardSummaryDto(
    decimal TotalPremiumProduction,
    int ActivePolicyCount,
    int PendingQuoteCount,
    int PendingClaimCount,
    decimal RenewalRate,
    decimal ClaimToPremiumRatio,
    IReadOnlyList<MonthlySalesPointDto> MonthlySales,
    IReadOnlyList<BranchDistributionPointDto> BranchDistribution);
