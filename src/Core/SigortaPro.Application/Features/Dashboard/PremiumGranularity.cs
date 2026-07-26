namespace SigortaPro.Application.Features.Dashboard;

// Prim üretimi zaman serisinin kova genişliği. Seçilen tarih aralığının uzunluğundan TÜRETİLİR
// (handler karar verir) — böylece "Bugün" tek noktalı anlamsız bir grafik üretmez, uzun aralıklar da
// yüzlerce nokta döndürmez.
public enum PremiumGranularity
{
    Hourly,
    Daily,
    Monthly
}
