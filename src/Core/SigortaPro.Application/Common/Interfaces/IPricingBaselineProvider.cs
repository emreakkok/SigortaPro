using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// Yerleşik (kod-sabit) baz tarife. Hiç PricingVersion yayınlanmadan önce ve
// bir teklif herhangi bir versiyona sabitlenmediğinde fiyatlama motoru bu değerleri kullanır.
// Admin ekranı, "Varsayılan" gibi anlamsız bir etiket yerine bu GERÇEK sayıları gösterebilsin diye
// baseline salt-okunur biçimde açılır. Değerlerin tek kaynağı Infrastructure'daki fiyatlama motorudur
// (arayüz Application, implementasyon Infrastructure).
public interface IPricingBaselineProvider
{
    // Branş bazlı yerleşik baz primler (TRY). Motorun kullandığı değerlerle birebir aynıdır.
    IReadOnlyDictionary<InsuranceBranch, decimal> BaselineBasePremiums { get; }

    // Yerleşik il risk katsayıları (motorun CityRiskFactor tablosuyla birebir). Yeni taslak versiyon
    // bu değerlerle seed edilir → admin, mevcut il katsayılarından başlar.
    IReadOnlyDictionary<string, decimal> BaselineCityRiskCoefficients { get; }

    // İl listede yoksa uygulanan yerleşik varsayılan katsayı.
    decimal BaselineDefaultCityRiskCoefficient { get; }

    // Bantlı aktüeryal faktörlerin yerleşik baseline değerleri (sürücü/araç/konut/sağlık). Yeni taslak
    // bunlarla seed edilir; bir versiyonda ilgili grup boşsa motor da DTO da bu değerleri kullanır.
    PricingBandBaselines BandBaselines { get; }
}
