using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Renewals;

// Süresi dolmakta olan poliçenin özgün teklifinden yeni bir dönem teklifi (Priced) kurar: aynı branş/ürün/risk
// objesi/teminat paketi, güncel referans tarihiyle yeniden fiyatlanır ve hasar geçmişi çarpanı uygulanır (Task 13).
internal static class RenewalQuoteFactory
{
    // sourceQuote: yenilenen poliçenin teklifi (yalnızca skaler alanları — branş/ürün/risk objesi/paket — okunur).
    // product/vehicle/property/customer fiyatlama için ayrıca verilir (navigation'a bağımlı değildir → izole test edilebilir).
    public static Quote Build(
        Quote sourceQuote,
        Customer customer,
        InsuranceProduct product,
        Vehicle? vehicle,
        Property? property,
        IPricingEngine pricingEngine,
        decimal claimHistoryFactor,
        DateTime now)
    {
        var pricing = QuotePricingFactory.Compute(
            pricingEngine, sourceQuote.Branch, customer, vehicle, property,
            product.Coverages, sourceQuote.CoveragePackage, now, claimHistoryFactor);

        var renewalQuote = new Quote(
            customer.Id, sourceQuote.InsuranceProductId, sourceQuote.Branch,
            sourceQuote.VehicleId, sourceQuote.PropertyId);

        renewalQuote.SelectCoveragePackage(sourceQuote.CoveragePackage);
        renewalQuote.ApplyClaimHistoryFactor(claimHistoryFactor);
        renewalQuote.MarkAsPriced(pricing.TotalPremium, now.AddDays(BusinessConstants.MaxQuoteValidityDays));

        return renewalQuote;
    }
}
