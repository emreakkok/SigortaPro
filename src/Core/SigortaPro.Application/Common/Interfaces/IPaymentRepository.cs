using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Ödeme modülüne özgü sorgular (ARCHITECTURE_RULES.md §4.2, ADR-005).
public interface IPaymentRepository : IReadRepository<Payment>, IWriteRepository<Payment>
{
    // Müşterinin ödeme geçmişi: en yeni işlem önce, sayfalanmış.
    Task<PagedResult<Payment>> GetByCustomerAsync(
        Guid customerId,
        PaginationParams paging,
        CancellationToken cancellationToken = default);
}
