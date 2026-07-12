using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Yenileme modülüne özgü sorgular (ARCHITECTURE_RULES.md §4.2, ADR-005).
public interface IRenewalRepository : IReadRepository<Renewal>, IWriteRepository<Renewal>
{
    // İzlemeli (tracked) yenileme — onay komutu için; yeni teklif (onaylanacak) ve poliçe ile birlikte.
    Task<Renewal?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Müşterinin yenileme teklifleri: yenilenen poliçe + yeni teklif ile, en yeni önce, sayfalanmış.
    Task<PagedResult<Renewal>> GetByCustomerAsync(
        Guid customerId,
        PaginationParams paging,
        CancellationToken cancellationToken = default);
}
