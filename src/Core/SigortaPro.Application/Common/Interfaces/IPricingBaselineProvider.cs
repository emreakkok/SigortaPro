using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// ADR-048/ADR-049: Yerleşik (kod-sabit) baz tarife. Hiç PricingVersion yayınlanmadan önce ve
// bir teklif herhangi bir versiyona sabitlenmediğinde fiyatlama motoru bu değerleri kullanır.
// Admin ekranı, "Varsayılan" gibi anlamsız bir etiket yerine bu GERÇEK sayıları gösterebilsin diye
// baseline salt-okunur biçimde açılır. Değerlerin tek kaynağı Infrastructure'daki fiyatlama motorudur
// (ARCHITECTURE_RULES §6.1: arayüz Application, implementasyon Infrastructure).
public interface IPricingBaselineProvider
{
    // Branş bazlı yerleşik baz primler (TRY). Motorun kullandığı değerlerle birebir aynıdır.
    IReadOnlyDictionary<InsuranceBranch, decimal> BaselineBasePremiums { get; }
}
