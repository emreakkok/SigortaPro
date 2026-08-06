namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// SQL tarafında (Claim GROUP BY müşteri) hesaplanan, en riskli müşteri segmentleri için hasar özeti.
// TotalClaimAmount = fiyatlamaya etki eden (Approved/Paid) hasarların onay tutarı toplamıdır. API DTO'su
// değil; IDashboardRepository'nin döndürdüğü salt okunur sorgu sonucudur (handler DTO'ya eşler).
public sealed record CustomerRiskAggregate(
    Guid CustomerId,
    string FirstName,
    string LastName,
    int ClaimCount,
    decimal TotalClaimAmount);
