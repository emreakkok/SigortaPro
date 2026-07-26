using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Pricing;

// ADR-049: Yerleşik baz tarifeyi tek kaynaktan (PricingRuleTables) açar. Motor da fiyatı buradan okur;
// böylece admin ekranındaki "yerleşik varsayılan" gösterimi ile gerçek hesaplama BİRBİRİNDEN ASLA
// SAPMAZ (frontend'de fiyat kopyalanmaz — sahte veri riski yoktur). Stateless → Singleton.
internal sealed class PricingBaselineProvider : IPricingBaselineProvider
{
    public IReadOnlyDictionary<InsuranceBranch, decimal> BaselineBasePremiums => PricingRuleTables.BasePremiums;
}
