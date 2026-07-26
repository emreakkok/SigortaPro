using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// ADR-048: Fiyatlandırma versiyonlarının okuma/yazma soyutlaması (ARCHITECTURE_RULES.md §4.2).
// Versiyonlar değişmezdir; güncelleme metodu bilinçli olarak yoktur (yeni değer = yeni versiyon).
public interface IPricingVersionRepository : IWriteRepository<PricingVersion>
{
    // Verilen ana yürürlükte olan tarife: EffectiveFrom <= asOf olanların en yenisi
    // (eşitlikte VersionNumber büyük olan). Hiç versiyon yoksa null → yerleşik baseline kullanılır.
    Task<PricingVersion?> GetEffectiveAsync(DateTime asOf, CancellationToken cancellationToken = default);

    // Teklifin sabitlediği versiyonu oranlarıyla getirir (deterministik yeniden hesap için).
    Task<PricingVersion?> GetWithRatesByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Fiyatlandırma geçmişi: tüm versiyonlar, en yeni önce (oranlarıyla birlikte).
    Task<IReadOnlyList<PricingVersion>> GetHistoryAsync(CancellationToken cancellationToken = default);

    // Bir sonraki sıra numarası (mevcut en büyük + 1; hiç yoksa 1).
    Task<int> GetNextVersionNumberAsync(CancellationToken cancellationToken = default);
}
