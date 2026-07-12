namespace SigortaPro.Application.Features.Renewals;

// Yenileme fiyatlamasında müşterinin hasar geçmişini prim çarpanına eşler (PRICING.md "Yenileme Hasar Çarpanı").
// Fiyatlamaya etki eden (Approved + Paid) her hasar ek prim getirir; belirli bir tavana kadar birikir.
// Hasarsız müşteride çarpan 1.00'dır (etkisiz). Değerler PRICING.md ile birebir eşleşir (ADR-025).
internal static class RenewalPricing
{
    private const decimal SurchargePerClaim = 0.20m;
    private const int MaxSurchargedClaims = 3; // en fazla +%60

    public static decimal ClaimHistoryFactor(int reportableClaimCount)
    {
        var effectiveCount = Math.Clamp(reportableClaimCount, 0, MaxSurchargedClaims);
        return 1.00m + (effectiveCount * SurchargePerClaim);
    }
}
