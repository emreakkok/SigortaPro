using SigortaPro.Application.Features.Dashboard.DTOs;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Dashboard;

// Entity/ReadModel → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
internal static class DashboardMappings
{
    public static MonthlySalesPointDto ToPointDto(MonthlySalesAggregate aggregate) => new(
        aggregate.Year,
        aggregate.Month,
        aggregate.PolicyCount,
        aggregate.PremiumTotal);

    public static BranchDistributionPointDto ToPointDto(BranchDistributionAggregate aggregate) => new(
        aggregate.Branch,
        aggregate.PolicyCount,
        aggregate.PremiumTotal);

    public static CustomerRiskSegmentDto ToSegmentDto(CustomerRiskAggregate aggregate) => new(
        aggregate.CustomerId,
        $"{aggregate.FirstName} {aggregate.LastName}",
        aggregate.ClaimCount,
        aggregate.TotalClaimAmount);

    public static PolicyReportItemDto ToReportItem(Policy policy) => new(
        policy.Id,
        policy.PolicyNumber,
        policy.Customer is null ? string.Empty : $"{policy.Customer.FirstName} {policy.Customer.LastName}",
        // Branş teklifte tutulur; rapor sorgusu teklifi Include eder. Beklenmedik biçimde yoksa varsayılan (0) branş.
        policy.Quote?.Branch ?? default,
        policy.Status,
        policy.StartDate,
        policy.EndDate,
        policy.TotalPremium);

    public static PaymentReportItemDto ToReportItem(Payment payment) => new(
        payment.Id,
        payment.CustomerId,
        payment.Customer is null ? string.Empty : $"{payment.Customer.FirstName} {payment.Customer.LastName}",
        payment.Amount,
        payment.InstallmentCount,
        payment.MaskedCardNumber,
        payment.Status,
        payment.TransactionDate);
}
