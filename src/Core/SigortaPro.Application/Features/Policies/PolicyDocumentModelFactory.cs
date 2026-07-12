using SigortaPro.Application.Common.Documents;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Security;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Policies;

// Yüklenmiş Policy aggregate'inden PDF sertifika modelini kurar. Prim dökümü ve ölçekli teminatlar,
// Task 8 motoru + Task 9 fiyatlama köprüsüyle teklifin CreatedAt referansıyla deterministik yeniden
// hesaplanır (ADR-021 ile tutarlı: teklif detayı ve poliçe sertifikası aynı primi/dökümü üretir).
internal static class PolicyDocumentModelFactory
{
    public static PolicyDocumentModel Build(Policy policy, IPricingEngine pricingEngine, DateTime issuedAt)
    {
        var quote = policy.Quote
            ?? throw new InvalidOperationException("Poliçe belgesi için teklif verisi yüklenmemiş.");
        var customer = policy.Customer
            ?? throw new InvalidOperationException("Poliçe belgesi için müşteri verisi yüklenmemiş.");
        var product = quote.InsuranceProduct
            ?? throw new InvalidOperationException("Poliçe belgesi için ürün verisi yüklenmemiş.");

        var pricing = QuotePricingFactory.Compute(
            pricingEngine, quote.Branch, customer, quote.Vehicle, quote.Property,
            product.Coverages, quote.CoveragePackage, quote.CreatedAt, quote.ClaimHistoryFactor);

        var riskObject = QuoteMappings.BuildRiskObject(quote.Vehicle, quote.Property);

        var coverages = pricing.Coverages
            .Select(coverage => new PolicyCoverageLine(coverage.Name, coverage.Description ?? string.Empty, coverage.Limit))
            .ToList();

        var address = customer.Address;

        return new PolicyDocumentModel(
            AgencyProfile.Name,
            AgencyProfile.Address,
            AgencyProfile.Contact,
            policy.PolicyNumber,
            policy.Status,
            policy.StartDate,
            policy.EndDate,
            issuedAt,
            $"{customer.FirstName} {customer.LastName}",
            SensitiveDataMasker.MaskTckn(customer.Tckn),
            customer.PhoneNumber,
            $"{address.Neighborhood} {address.District}/{address.City} {address.PostalCode}".Trim(),
            quote.Branch,
            product.Name,
            quote.CoveragePackage,
            riskObject.Kind,
            riskObject.Display,
            pricing.RiskScore,
            pricing.BasePremium,
            policy.TotalPremium,
            coverages,
            pricing.Breakdown);
    }
}
