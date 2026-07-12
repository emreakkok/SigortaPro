using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IRenewalRepository implementasyonu (ADR-005, ARCHITECTURE_RULES.md §4.2).
public sealed class RenewalRepository : GenericRepository<Renewal>, IRenewalRepository
{
    private readonly AppDbContext _context;

    public RenewalRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    // İzlemeli: onay komutu için yeni teklif (Approve edilecek) ve poliçe (numara/gösterim) ile birlikte.
    public Task<Renewal?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Renewals
            .Include(renewal => renewal.NewQuote)
            .Include(renewal => renewal.Policy)
            .FirstOrDefaultAsync(renewal => renewal.Id == id, cancellationToken);

    public async Task<PagedResult<Renewal>> GetByCustomerAsync(
        Guid customerId,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Renewals
            .AsNoTracking()
            .Include(renewal => renewal.Policy)
            .Include(renewal => renewal.NewQuote)
            .Where(renewal => renewal.Policy!.CustomerId == customerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(renewal => renewal.OfferedAt)
            .ThenBy(renewal => renewal.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Renewal>(items, paging.Page, paging.PageSize, totalCount);
    }
}
