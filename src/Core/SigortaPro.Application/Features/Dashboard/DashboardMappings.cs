using SigortaPro.Application.Features.Dashboard.DTOs;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Dashboard;

// Entity/ReadModel → DTO manuel eşlemeleri (AutoMapper kullanılmaz).
internal static class DashboardMappings
{
    public static PremiumSeriesPointDto ToPointDto(PremiumSeriesAggregate aggregate) => new(
        aggregate.BucketStart,
        aggregate.PolicyCount,
        aggregate.PremiumTotal);

    // Dönüşüm oranı payda 0 iken null döner — "%0" göstermek yanıltıcı olurdu (teklif yoksa oran tanımsızdır).
    public static BranchPerformanceDto ToPerformanceDto(BranchPerformanceAggregate aggregate) => new(
        aggregate.Branch,
        aggregate.QuoteCount,
        aggregate.PurchasedCount,
        aggregate.PremiumTotal,
        Ratio(aggregate.PurchasedCount, aggregate.QuoteCount));

    /// <summary>Oranı 4 ondalığa yuvarlar; payda 0 ise <c>null</c> (tanımsız) döner.</summary>
    public static decimal? Ratio(decimal numerator, decimal denominator) =>
        denominator == 0m ? null : Math.Round(numerator / denominator, 4, MidpointRounding.AwayFromZero);

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
        policy.TotalPremium,
        // Additive: aynı isimli müşterileri ayırt etmek için telefon + stabil CustomerId (rapor sorgusu Customer Include eder).
        policy.CustomerId,
        policy.Customer?.PhoneNumber);

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
