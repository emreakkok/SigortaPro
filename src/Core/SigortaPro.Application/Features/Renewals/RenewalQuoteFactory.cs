using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Renewals;

// Süresi dolmakta olan poliçenin özgün teklifinden yeni bir dönem teklifi (Priced) kurar: aynı branş/ürün/risk
// objesi/teminat paketi, güncel referans tarihiyle yeniden fiyatlanır ve hasar geçmişi çarpanı uygulanır.
internal static class RenewalQuoteFactory
{
    // sourceQuote: yenilenen poliçenin teklifi (yalnızca skaler alanları — branş/ürün/risk objesi/paket — okunur).
    // product/vehicle/property/customer fiyatlama için ayrıca verilir (navigation'a bağımlı değildir → izole test edilebilir).
    // effectivePricing: yenileme YENİ bir dönem teklifi ürettiğinden, güncel referans tarihiyle
    // birlikte O AN yürürlükteki tarife kullanılır ve yeni teklifte sabitlenir. Kaynak teklifin/poliçenin
    // fiyatı değişmez — yalnızca yeni dönem teklifi güncel tarifeyle fiyatlanır.
    public static Quote Build(
        Quote sourceQuote,
        Customer customer,
        InsuranceProduct product,
        Vehicle? vehicle,
        Property? property,
        IPricingEngine pricingEngine,
        DateTime now,
        DateTime policyEndDate,
        PricingSnapshot snapshot,
        EffectivePricing? effectivePricing = null)
    {
        // Girdi (snapshot) çağıran handler tarafından ORTAK IQuotePricingInputBuilder ile
        // kurulur ve buraya hazır verilir → yenileme, teklif oluşturma ve önizleme aynı yolu kullanır.
        // Hasar geçmişi artık bu snapshot'taki Bonus-Malus basamağıyla fiyatlanır; ayrı bir
        // ClaimHistoryFactor UYGULANMAZ (yeni tekliflerde 1.00 = nötr kalır).
        // Yenileme indirimi AKTİF tarifeden okunur (gerçek sigortacılıkta yenileme yeni tarifeyle fiyatlanır).
        // 1.00 = indirim yok. Değer teklifte dondurulacağından yeniden hesap deterministiktir.
        var renewalDiscount = effectivePricing?.Rates?.RuleSet?.RenewalDiscountFactor ?? 1.00m;

        var pricing = QuotePricingFactory.Compute(
            pricingEngine, sourceQuote.Branch, customer, vehicle, property,
            product.Coverages, sourceQuote.CoveragePackage, now,
            insuredBirthDate: sourceQuote.InsuredPerson?.BirthDate,
            rates: effectivePricing?.Rates,
            snapshot: snapshot,
            renewalDiscountFactor: renewalDiscount);

        var renewalQuote = new Quote(
            customer.Id, sourceQuote.InsuranceProductId, sourceQuote.Branch,
            sourceQuote.VehicleId, sourceQuote.PropertyId);

        renewalQuote.SelectCoveragePackage(sourceQuote.CoveragePackage);
        renewalQuote.CapturePricingSnapshot(snapshot);

        if (effectivePricing?.VersionId is not null)
        {
            renewalQuote.PinPricingVersion(effectivePricing.VersionId.Value);
        }

        // "başkası adına" sağlık poliçesinin yenilemesi aynı sigortalı için düzenlenir
        // (owned instance entity'ler arasında paylaşılmaz; kaynak beyandan kopyalanır).
        if (sourceQuote.InsuredPerson is not null)
        {
            renewalQuote.SetInsuredPerson(new Domain.Entities.InsuredPerson(
                sourceQuote.InsuredPerson.FirstName,
                sourceQuote.InsuredPerson.LastName,
                sourceQuote.InsuredPerson.Tckn,
                sourceQuote.InsuredPerson.BirthDate,
                sourceQuote.InsuredPerson.PhoneNumber,
                sourceQuote.InsuredPerson.Relationship));
        }
        // Yenileme indirimini teklifte dondur (yalnızca etkiliyse) — yeniden hesap aynı değeri kullanır.
        if (renewalDiscount != 1.00m)
        {
            renewalQuote.ApplyRenewalDiscount(renewalDiscount);
        }

        // Yenileme teklifinin geçerliliği (kabul son tarihi) poliçe bitişine dayanır:
        // müşteri mevcut poliçesi sona erene kadar kabul edebilmelidir. Böylece teklif, poliçe hâlâ AKTİFken
        // "geçerlilik süresi doldu" görünmez. Normal (yenileme dışı) tekliflerin 7 günlük vadesi değişmez.
        renewalQuote.MarkAsPriced(pricing.TotalPremium, RenewalOfferValidity.Compute(now, policyEndDate));

        return renewalQuote;
    }
}
