using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Ödeme modülüne özgü sorgular.
public interface IPaymentRepository : IReadRepository<Payment>, IWriteRepository<Payment>
{
    // Müşterinin ödeme geçmişi: en yeni işlem önce, sayfalanmış.
    Task<PagedResult<Payment>> GetByCustomerAsync(
        Guid customerId,
        PaginationParams paging,
        CancellationToken cancellationToken = default);
}
