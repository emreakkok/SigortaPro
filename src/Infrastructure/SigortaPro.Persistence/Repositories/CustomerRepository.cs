using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// ICustomerRepository implementasyonu (ADR-005, ARCHITECTURE_RULES.md §4.2). Generic CRUD'u
// GenericRepository'den devralır; müşteri modülüne özgü include/arama sorgularını EF Core ile ekler.
public sealed class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<Customer?> GetProfileByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default) =>
        _context.Customers
            .AsNoTracking()
            .Include(customer => customer.Vehicles)
            .Include(customer => customer.Properties)
            .FirstOrDefaultAsync(customer => customer.AppUserId == appUserId, cancellationToken);

    public Task<Customer?> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Customers
            .AsNoTracking()
            .Include(customer => customer.Vehicles)
            .Include(customer => customer.Properties)
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    // Güncelleme komutlarında oturum sahibinin müşterisini izlemeli çözümler; yalın tutulur (include yok).
    public Task<Customer?> GetTrackedByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default) =>
        _context.Customers
            .FirstOrDefaultAsync(customer => customer.AppUserId == appUserId, cancellationToken);

    // Kayıt akışı TCKN ön-kontrolü. UQ_Customers_TCKN filtresiz olduğundan soft-delete edilmiş kayıtlar da
    // TCKN'i işgal eder; DB unique ihlaliyle birebir örtüşmesi için query filter yok sayılır.
    public Task<bool> ExistsByTcknAsync(string tckn, CancellationToken cancellationToken = default) =>
        _context.Customers
            .IgnoreQueryFilters()
            .AnyAsync(customer => customer.Tckn == tckn, cancellationToken);

    public async Task<PagedResult<Customer>> SearchAsync(
        string? searchTerm,
        string? city,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(customer =>
                customer.FirstName.Contains(term)
                || customer.LastName.Contains(term)
                || customer.Tckn.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityFilter = city.Trim();
            query = query.Where(customer => customer.Address.City == cityFilter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Customer>(items, paging.Page, paging.PageSize, totalCount);
    }
}
