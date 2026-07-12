using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Poliçe modülüne özgü sorgular (ARCHITECTURE_RULES.md §4.2, ADR-005).
public interface IPolicyRepository : IReadRepository<Policy>, IWriteRepository<Policy>
{
    // Verilen yıla ait mevcut poliçe sayısı — sıralı poliçe numarası üretiminde kullanılır (ADR-022).
    Task<int> CountByYearAsync(int year, CancellationToken cancellationToken = default);

    // İzlemeli (tracked) poliçe detayı: müşteri, teklif (ürün+teminatlar, risk objesi) ve varsa belge ile.
    // PDF üretiminde belge kaydı eklenip kaydedilebildiği için izlemelidir (ADR-023).
    Task<Policy?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // İzlemeli: bitiş tarihi geçmiş aktif poliçeler — arkaplan servisi bunları Expired'a çeker (Task 13).
    Task<IReadOnlyList<Policy>> GetOverdueActiveAsync(DateTime asOf, CancellationToken cancellationToken = default);

    // Salt okunur: bitişine yenileme penceresi kadar (≤30 gün) kalan, henüz yenileme teklifi olmayan aktif
    // poliçeler — teklif (ürün+teminatlar, risk objesi) ve müşteri ile birlikte (yenileme fiyatlaması için, Task 13).
    Task<IReadOnlyList<Policy>> GetDueForRenewalAsync(
        DateTime asOf,
        DateTime renewalWindowEnd,
        CancellationToken cancellationToken = default);
}
