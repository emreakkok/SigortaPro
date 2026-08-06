namespace SigortaPro.Application.Common.Pricing;

// Deprem bölgesiyle ilgili ortak sabitler. Önceden "bilinmeyen bölge" sentinel'i birden fazla
// yerde ayrı ayrı tanımlıydı; tek kaynağa alınarak yolların sessizce ayrışması engellenir.
public static class EarthquakeZoneDefaults
{
    /// <summary>
    /// Adresin ilinden bölge çözülemediğinde kullanılan sentinel. Geçerli bölge aralığı 1–5 olduğundan
    /// 0 "bilinmiyor" anlamına gelir ve fiyatlama motoru bunu açık açıklamasıyla "bilinmeyen bölge"
    /// (orta risk) olarak ele alır — sessizce (ve müşteri lehine) bir bölge ATANMAZ.
    /// </summary>
    public const int Unknown = 0;
}
