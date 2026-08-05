using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// ADR-048: Fiyatlandırma versiyonlarının okuma/yazma soyutlaması (ARCHITECTURE_RULES.md §4.2).
// Aktif/arşiv versiyonlar değişmezdir; yalnızca TASLAK düzenlenir (yeni değer = yeni versiyon / taslak).
public interface IPricingVersionRepository : IWriteRepository<PricingVersion>
{
    // Şu an YÜRÜRLÜKTEKİ (Active) tarife — yeni tekliflerde kullanılır. Hiç aktif versiyon yoksa null →
    // yerleşik baseline kullanılır (temiz kurulum / ilk tarife aktifleştirilene kadar).
    Task<PricingVersion?> GetActiveAsync(CancellationToken cancellationToken = default);

    // Açık taslak (Draft) varsa getirir — aynı anda birden fazla taslağın oluşmasını engellemek için.
    Task<PricingVersion?> GetDraftAsync(CancellationToken cancellationToken = default);

    // İZLEMELİ taslak (oranlarıyla) — taslak düzenleme/aktifleştirme komutları için.
    Task<PricingVersion?> GetTrackedWithRatesByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Teklifin sabitlediği versiyonu oranlarıyla getirir (deterministik yeniden hesap için).
    Task<PricingVersion?> GetWithRatesByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Fiyatlandırma geçmişi: tüm versiyonlar, en yeni önce (oranlarıyla birlikte).
    Task<IReadOnlyList<PricingVersion>> GetHistoryAsync(CancellationToken cancellationToken = default);

    // Bir sonraki sıra numarası (mevcut en büyük + 1; hiç yoksa 1).
    Task<int> GetNextVersionNumberAsync(CancellationToken cancellationToken = default);
}
