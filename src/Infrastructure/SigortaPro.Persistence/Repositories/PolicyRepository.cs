using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IPolicyRepository implementasyonu (ADR-005, ARCHITECTURE_RULES.md §4.2).
public sealed class PolicyRepository : GenericRepository<Policy>, IPolicyRepository
{
    private readonly AppDbContext _context;

    public PolicyRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    // Yıla ait poliçe sayısı (sıralı numara üretimi — ADR-022). Soft-delete edilmiş poliçeler de benzersiz
    // numarayı işgal ettiğinden numara tekrarını önlemek için query filter yok sayılır (IgnoreQueryFilters).
    public Task<int> CountByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"{BusinessConstants.PolicyNumberPrefix}-{year}-";

        return _context.Policies
            .IgnoreQueryFilters()
            .CountAsync(policy => policy.PolicyNumber.StartsWith(prefix), cancellationToken);
    }

    // İzlemeli detay (PDF üretiminde belge eklenip kaydedilebilir — ADR-023): müşteri, teklif (ürün+teminatlar,
    // araç/konut) ve varsa mevcut belge yüklenir.
    public Task<Policy?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Policies
            .Include(policy => policy.Customer)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.InsuranceProduct!).ThenInclude(product => product.Coverages)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Vehicle)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Property)
            .Include(policy => policy.PolicyDocument)
            .FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    // İzlemeli: bitiş tarihi geçmiş aktif poliçeler (arkaplan expiry — Task 13).
    public async Task<IReadOnlyList<Policy>> GetOverdueActiveAsync(DateTime asOf, CancellationToken cancellationToken = default) =>
        await _context.Policies
            .Where(policy => policy.Status == PolicyStatus.Active && policy.EndDate < asOf)
            .ToListAsync(cancellationToken);

    // Salt okunur: bitişine ≤ pencere kadar kalan, henüz yenileme teklifi olmayan aktif poliçeler; özgün teklif
    // (ürün+teminatlar, risk objesi) ve müşteri ile birlikte (yenileme fiyatlaması için, Task 13).
    public async Task<IReadOnlyList<Policy>> GetDueForRenewalAsync(
        DateTime asOf,
        DateTime renewalWindowEnd,
        CancellationToken cancellationToken = default) =>
        await _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Customer)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.InsuranceProduct!).ThenInclude(product => product.Coverages)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Vehicle)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Property)
            .Where(policy => policy.Status == PolicyStatus.Active
                && policy.EndDate >= asOf
                && policy.EndDate <= renewalWindowEnd
                && !policy.Renewals.Any())
            .ToListAsync(cancellationToken);
}
