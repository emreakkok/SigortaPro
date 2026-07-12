using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IQuoteRepository implementasyonu (ADR-005, ARCHITECTURE_RULES.md §4.2).
public sealed class QuoteRepository : GenericRepository<Quote>, IQuoteRepository
{
    private readonly AppDbContext _context;

    public QuoteRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<Quote?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Quotes
            .AsNoTracking()
            .Include(quote => quote.Customer)
            .Include(quote => quote.InsuranceProduct!).ThenInclude(product => product.Coverages)
            .Include(quote => quote.Vehicle)
            .Include(quote => quote.Property)
            .FirstOrDefaultAsync(quote => quote.Id == id, cancellationToken);

    // Durum geçişi komutları için izlemeli teklif; özet yanıtta ürün adı için InsuranceProduct yüklenir.
    // (Sahiplik kontrolü CustomerId skaleri üzerinden yapılır; Customer navigasyonu gerekmez.)
    public Task<Quote?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Quotes
            .Include(quote => quote.InsuranceProduct)
            .FirstOrDefaultAsync(quote => quote.Id == id, cancellationToken);

    public async Task<PagedResult<Quote>> SearchAsync(
        Guid? customerId,
        QuoteStatus? status,
        InsuranceBranch? branch,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Quotes
            .AsNoTracking()
            .Include(quote => quote.InsuranceProduct)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(quote => quote.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(quote => quote.Status == status.Value);
        }

        if (branch.HasValue)
        {
            query = query.Where(quote => quote.Branch == branch.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(quote => quote.CreatedAt)
            .ThenBy(quote => quote.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Quote>(items, paging.Page, paging.PageSize, totalCount);
    }

    // İzlemeli: geçerlilik süresi dolmuş ancak henüz sonlanmamış teklifler (arkaplan expiry — Task 13).
    // Yalnızca Expire guard'ının kabul ettiği durumlar döner (Purchased/Rejected/Expired hariç).
    public async Task<IReadOnlyList<Quote>> GetExpirableAsync(DateTime asOf, CancellationToken cancellationToken = default) =>
        await _context.Quotes
            .Where(quote => quote.ValidUntil != null && quote.ValidUntil < asOf
                && (quote.Status == QuoteStatus.Draft
                    || quote.Status == QuoteStatus.Priced
                    || quote.Status == QuoteStatus.Approved))
            .ToListAsync(cancellationToken);
}
