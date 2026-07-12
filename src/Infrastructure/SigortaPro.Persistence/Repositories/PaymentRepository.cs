using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IPaymentRepository implementasyonu (ADR-005, ARCHITECTURE_RULES.md §4.2).
public sealed class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PagedResult<Payment>> GetByCustomerAsync(
        Guid customerId,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(payment => payment.CustomerId == customerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(payment => payment.TransactionDate)
            .ThenBy(payment => payment.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>(items, paging.Page, paging.PageSize, totalCount);
    }
}
