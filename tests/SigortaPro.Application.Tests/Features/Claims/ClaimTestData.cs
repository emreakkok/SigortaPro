using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Claims;

// Hasar modülü testlerinde kullanılan entity kurucuları.
internal static class ClaimTestData
{
    public static Policy CreateActivePolicy(Guid customerId, DateTime startDate, DateTime endDate) =>
        new("POL-2026-000007", customerId, Guid.NewGuid(), startDate, endDate, 20000m);

    public static Claim SubmittedClaim(Guid policyId, Guid customerId, DateTime incidentDate) =>
        new(policyId, customerId, incidentDate, "Ön tamponda hasar oluştu.", 5000m);

    public static Claim UnderReviewClaim(Guid policyId, Guid customerId, DateTime incidentDate)
    {
        var claim = SubmittedClaim(policyId, customerId, incidentDate);
        claim.StartReview();
        return claim;
    }

    public static Claim ApprovedClaim(Guid policyId, Guid customerId, DateTime incidentDate)
    {
        var claim = UnderReviewClaim(policyId, customerId, incidentDate);
        claim.Approve(4000m, "Hasar tespiti onaylandı.");
        return claim;
    }
}
