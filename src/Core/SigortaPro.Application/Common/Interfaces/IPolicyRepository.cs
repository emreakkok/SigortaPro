using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// Poliçe modülüne özgü sorgular (ARCHITECTURE_RULES.md §4.2, ADR-005).
public interface IPolicyRepository : IReadRepository<Policy>, IWriteRepository<Policy>
{
    // Verilen yıla ait mevcut poliçe sayısı — sıralı poliçe numarası üretiminde kullanılır (ADR-022).
    Task<int> CountByYearAsync(int year, CancellationToken cancellationToken = default);

    // Salt okunur, sayfalanmış "Poliçelerim" listesi: yalnızca ilgili müşterinin poliçeleri; opsiyonel durum
    // filtresi (aktif/pasif). Branş/ürün adı için teklif+ürün (InsuranceProduct) yüklenir.
    Task<PagedResult<Policy>> GetByCustomerPagedAsync(
        Guid customerId,
        PolicyStatus? status,
        PaginationParams paging,
        CancellationToken cancellationToken = default);

    // Salt okunur (AsNoTracking) poliçe detayı: müşteri, teklif (ürün+teminatlar, araç/konut) ile birlikte —
    // teminat tablosunun deterministik yeniden hesabı için (GetPolicyById). PDF üretimi için tracked
    // GetDetailByIdAsync ayrıdır (ADR-023).
    Task<Policy?> GetReadDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

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

    // ADR-059: Bonus-Malus girdisi — aynı branşta HASARSIZ tamamlanmış poliçe dönemi sayısı.
    // "Tamamlanmış" = dönemi biten (EndDate < asOf); iptal edilen poliçeler bonus kazandırmaz.
    // "Hasarsız" = o poliçeye bağlı onaylanmış/ödenmiş hasar bulunmayan.
    Task<int> CountClaimFreeCompletedPeriodsAsync(
        Guid customerId, InsuranceBranch branch, DateTime asOf, CancellationToken cancellationToken = default);
}
