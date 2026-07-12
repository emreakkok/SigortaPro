using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IDashboardRepository implementasyonu (ADR-005 §4.2, ADR-026). Tüm sorgular salt okunur (AsNoTracking);
// metrikler SQL tarafında agregasyon (COUNT/SUM/GROUP BY) ile hesaplanır — entity materialize edilmez,
// N+1 üretilmez. Hiçbir metot durum değiştirmez.
public sealed class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalPremiumProductionAsync(CancellationToken cancellationToken = default) =>
        await _context.Policies
            .AsNoTracking()
            .SumAsync(policy => (decimal?)policy.TotalPremium, cancellationToken) ?? 0m;

    public Task<int> GetActivePolicyCountAsync(CancellationToken cancellationToken = default) =>
        _context.Policies
            .AsNoTracking()
            .CountAsync(policy => policy.Status == PolicyStatus.Active, cancellationToken);

    public Task<int> GetPendingQuoteCountAsync(CancellationToken cancellationToken = default) =>
        _context.Quotes
            .AsNoTracking()
            .CountAsync(
                quote => quote.Status == QuoteStatus.Priced || quote.Status == QuoteStatus.Approved,
                cancellationToken);

    public Task<int> GetPendingClaimCountAsync(CancellationToken cancellationToken = default) =>
        _context.Claims
            .AsNoTracking()
            .CountAsync(
                claim => claim.Status == ClaimStatus.Submitted || claim.Status == ClaimStatus.UnderReview,
                cancellationToken);

    public async Task<decimal> GetTotalPaidClaimAmountAsync(CancellationToken cancellationToken = default) =>
        await _context.Claims
            .AsNoTracking()
            .Where(claim => claim.Status == ClaimStatus.Paid)
            .SumAsync(claim => claim.ApprovedAmount, cancellationToken) ?? 0m;

    public Task<int> GetRenewalOfferedCountAsync(CancellationToken cancellationToken = default) =>
        _context.Renewals
            .AsNoTracking()
            .CountAsync(cancellationToken);

    public Task<int> GetAcceptedRenewalCountAsync(CancellationToken cancellationToken = default) =>
        _context.Renewals
            .AsNoTracking()
            .CountAsync(renewal => renewal.IsAccepted, cancellationToken);

    public async Task<IReadOnlyList<MonthlySalesAggregate>> GetMonthlySalesTrendAsync(
        DateTime fromInclusive, CancellationToken cancellationToken = default)
    {
        // GROUP BY yıl/ay + COUNT/SUM SQL'de yürür; sonuç anonim projeksiyonla çekilip read-model'e eşlenir.
        var rows = await _context.Policies
            .AsNoTracking()
            .Where(policy => policy.CreatedAt >= fromInclusive)
            .GroupBy(policy => new { policy.CreatedAt.Year, policy.CreatedAt.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                PolicyCount = group.Count(),
                PremiumTotal = group.Sum(policy => policy.TotalPremium)
            })
            .OrderBy(row => row.Year).ThenBy(row => row.Month)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new MonthlySalesAggregate(row.Year, row.Month, row.PolicyCount, row.PremiumTotal))
            .ToList();
    }

    public async Task<IReadOnlyList<BranchDistributionAggregate>> GetBranchDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        // Branş teklifte tutulur; poliçe→teklif join'i ile branşa göre gruplanır (SQL tarafı agregasyon).
        var rows = await _context.Policies
            .AsNoTracking()
            .Where(policy => policy.Quote != null)
            .GroupBy(policy => policy.Quote!.Branch)
            .Select(group => new
            {
                Branch = group.Key,
                PolicyCount = group.Count(),
                PremiumTotal = group.Sum(policy => policy.TotalPremium)
            })
            .OrderBy(row => row.Branch)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new BranchDistributionAggregate(row.Branch, row.PolicyCount, row.PremiumTotal))
            .ToList();
    }

    public async Task<PagedResult<Policy>> GetPoliciesByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, PaginationParams paging, CancellationToken cancellationToken = default)
    {
        var query = _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Customer)
            .Include(policy => policy.Quote)
            .Where(policy => policy.StartDate >= fromInclusive && policy.StartDate <= toInclusive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(policy => policy.StartDate)
            .ThenBy(policy => policy.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Policy>(items, paging.Page, paging.PageSize, totalCount);
    }

    public async Task<PagedResult<Payment>> GetPaymentsByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, PaginationParams paging, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Customer)
            .Where(payment => payment.TransactionDate >= fromInclusive && payment.TransactionDate <= toInclusive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(payment => payment.TransactionDate)
            .ThenBy(payment => payment.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>(items, paging.Page, paging.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<CustomerRiskAggregate>> GetRiskiestCustomerSegmentsAsync(
        int topCount, CancellationToken cancellationToken = default)
    {
        // Hasarlar müşteriye göre gruplanır (Customer join'iyle ad/soyad); hasar sayısı, ardından fiyatlamaya
        // etki eden (Approved/Paid) hasar tutarı azalan sıralanır; ilk N kayıt SQL tarafında (Take) alınır.
        var rows = await _context.Claims
            .AsNoTracking()
            .GroupBy(claim => new { claim.CustomerId, claim.Customer!.FirstName, claim.Customer!.LastName })
            .Select(group => new
            {
                group.Key.CustomerId,
                group.Key.FirstName,
                group.Key.LastName,
                ClaimCount = group.Count(),
                TotalClaimAmount = group.Sum(claim =>
                    (claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.Paid)
                        ? (claim.ApprovedAmount ?? 0m)
                        : 0m)
            })
            .OrderByDescending(row => row.ClaimCount)
            .ThenByDescending(row => row.TotalClaimAmount)
            .Take(topCount)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CustomerRiskAggregate(
                row.CustomerId, row.FirstName, row.LastName, row.ClaimCount, row.TotalClaimAmount))
            .ToList();
    }
}
