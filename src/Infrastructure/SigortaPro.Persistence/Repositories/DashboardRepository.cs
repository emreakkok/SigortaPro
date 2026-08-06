using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Common.Search;
using SigortaPro.Application.Features.Dashboard;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IDashboardRepository implementasyonu. Tüm sorgular salt okunur (AsNoTracking);
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

    public Task<int> GetTotalCustomerCountAsync(CancellationToken cancellationToken = default) =>
        _context.Customers.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> GetUpcomingRenewalCountAsync(
        DateTime asOf, int withinDays, CancellationToken cancellationToken = default)
    {
        var horizon = asOf.AddDays(withinDays);
        return _context.Policies
            .AsNoTracking()
            .CountAsync(
                policy => policy.Status == PolicyStatus.Active
                    && policy.EndDate >= asOf
                    && policy.EndDate <= horizon,
                cancellationToken);
    }

    public Task<int> GetFailedPaymentCountAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default) =>
        _context.Payments
            .AsNoTracking()
            .CountAsync(
                payment => payment.Status == PaymentStatus.Failed
                    && payment.TransactionDate >= fromInclusive
                    && payment.TransactionDate <= toInclusive,
                cancellationToken);

    public async Task<PeriodStatsAggregate> GetPeriodStatsAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default)
    {
        // Her sayaç ayrı tabloda olduğundan ayrı COUNT/SUM sorgusu yürür; hiçbiri tabloyu belleğe çekmez.
        var newCustomers = await _context.Customers.AsNoTracking()
            .CountAsync(c => c.CreatedAt >= fromInclusive && c.CreatedAt <= toInclusive, cancellationToken);

        var newQuotes = await _context.Quotes.AsNoTracking()
            .CountAsync(q => q.CreatedAt >= fromInclusive && q.CreatedAt <= toInclusive, cancellationToken);

        var policies = _context.Policies.AsNoTracking()
            .Where(p => p.CreatedAt >= fromInclusive && p.CreatedAt <= toInclusive);

        var newPolicies = await policies.CountAsync(cancellationToken);
        var premium = await policies.SumAsync(p => (decimal?)p.TotalPremium, cancellationToken) ?? 0m;

        var newClaims = await _context.Claims.AsNoTracking()
            .CountAsync(c => c.CreatedAt >= fromInclusive && c.CreatedAt <= toInclusive, cancellationToken);

        return new PeriodStatsAggregate(newCustomers, newQuotes, newPolicies, newClaims, premium);
    }

    public async Task<QuoteFunnelAggregate> GetQuoteFunnelAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default)
    {
        // Tek GROUP BY: aralıkta oluşturulan tekliflerin GÜNCEL durum dağılımı (Draft kalıcı değildir).
        var rows = await _context.Quotes
            .AsNoTracking()
            .Where(quote => quote.CreatedAt >= fromInclusive && quote.CreatedAt <= toInclusive)
            .GroupBy(quote => quote.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(QuoteStatus status) => rows.FirstOrDefault(row => row.Status == status)?.Count ?? 0;

        return new QuoteFunnelAggregate(
            Priced: CountOf(QuoteStatus.Priced),
            Approved: CountOf(QuoteStatus.Approved),
            Purchased: CountOf(QuoteStatus.Purchased),
            Expired: CountOf(QuoteStatus.Expired),
            Rejected: CountOf(QuoteStatus.Rejected));
    }

    public async Task<IReadOnlyList<BranchPerformanceAggregate>> GetBranchPerformanceAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default)
    {
        // Tek kohort/tek sorgu: aralıkta oluşturulan teklifler branşa göre gruplanır; poliçeleşen (Purchased)
        // adet ve primi koşullu SUM ile hesaplanır → dönüşüm oranı asla %100'ü aşamaz.
        var rows = await _context.Quotes
            .AsNoTracking()
            .Where(quote => quote.CreatedAt >= fromInclusive && quote.CreatedAt <= toInclusive)
            .GroupBy(quote => quote.Branch)
            .Select(group => new
            {
                Branch = group.Key,
                QuoteCount = group.Count(),
                PurchasedCount = group.Count(quote => quote.Status == QuoteStatus.Purchased),
                PremiumTotal = group.Sum(quote =>
                    quote.Status == QuoteStatus.Purchased ? quote.TotalPremium : 0m)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new BranchPerformanceAggregate(
                row.Branch, row.QuoteCount, row.PurchasedCount, row.PremiumTotal))
            .OrderByDescending(row => row.PremiumTotal)
            .ThenByDescending(row => row.QuoteCount)
            .ToList();
    }

    public async Task<IReadOnlyList<ClaimStatusCountAggregate>> GetClaimStatusBreakdownAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default)
    {
        var rows = await _context.Claims
            .AsNoTracking()
            .Where(claim => claim.CreatedAt >= fromInclusive && claim.CreatedAt <= toInclusive)
            .GroupBy(claim => claim.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
                EstimatedTotal = group.Sum(claim => claim.EstimatedAmount),
                // Onay tutarı yalnızca girilmiş kayıtlarda anlamlıdır; yoksa 0 sayılır (uydurma yapılmaz).
                ApprovedTotal = group.Sum(claim => claim.ApprovedAmount ?? 0m)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ClaimStatusCountAggregate(
                row.Status, row.Count, row.EstimatedTotal, row.ApprovedTotal))
            .ToList();
    }

    public async Task<IReadOnlyList<PremiumSeriesAggregate>> GetPremiumSeriesAsync(
        DateTime fromInclusive,
        DateTime toInclusive,
        PremiumGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        // Seri ÜRETİM tarihine (Policy.CreatedAt) göredir — satış performansı ölçülür.
        var scoped = _context.Policies
            .AsNoTracking()
            .Where(policy => policy.CreatedAt >= fromInclusive && policy.CreatedAt <= toInclusive);

        // GROUP BY tamamen SQL'de yürür (CAST/DATEPART); yalnızca kova satırları materialize edilir.
        List<PremiumSeriesAggregate> series = granularity switch
        {
            PremiumGranularity.Hourly => (await scoped
                .GroupBy(policy => new { policy.CreatedAt.Date, policy.CreatedAt.Hour })
                .Select(group => new
                {
                    group.Key.Date,
                    group.Key.Hour,
                    PolicyCount = group.Count(),
                    PremiumTotal = group.Sum(policy => policy.TotalPremium)
                })
                .ToListAsync(cancellationToken))
                .Select(row => new PremiumSeriesAggregate(
                    row.Date.AddHours(row.Hour), row.PolicyCount, row.PremiumTotal))
                .ToList(),

            PremiumGranularity.Monthly => (await scoped
                .GroupBy(policy => new { policy.CreatedAt.Year, policy.CreatedAt.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    PolicyCount = group.Count(),
                    PremiumTotal = group.Sum(policy => policy.TotalPremium)
                })
                .ToListAsync(cancellationToken))
                .Select(row => new PremiumSeriesAggregate(
                    new DateTime(row.Year, row.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    row.PolicyCount,
                    row.PremiumTotal))
                .ToList(),

            _ => (await scoped
                .GroupBy(policy => policy.CreatedAt.Date)
                .Select(group => new
                {
                    Day = group.Key,
                    PolicyCount = group.Count(),
                    PremiumTotal = group.Sum(policy => policy.TotalPremium)
                })
                .ToListAsync(cancellationToken))
                .Select(row => new PremiumSeriesAggregate(row.Day, row.PolicyCount, row.PremiumTotal))
                .ToList(),
        };

        return series.OrderBy(point => point.BucketStart).ToList();
    }

    public async Task<(int Offered, int Accepted)> GetRenewalCountsAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default)
    {
        // Dönemsel yenileme oranı: aralıkta SUNULAN yenilemeler paydadır (kabul, sonradan gelmiş olabilir).
        var offeredInPeriod = _context.Renewals
            .AsNoTracking()
            .Where(renewal => renewal.OfferedAt >= fromInclusive && renewal.OfferedAt <= toInclusive);

        var offered = await offeredInPeriod.CountAsync(cancellationToken);
        var accepted = await offeredInPeriod.CountAsync(renewal => renewal.IsAccepted, cancellationToken);

        return (offered, accepted);
    }

    public async Task<PagedResult<Policy>> GetPoliciesByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, string? search, PaginationParams paging, CancellationToken cancellationToken = default)
    {
        // Customer/Quote zaten müşteri adı + branş için JOIN edilir (N+1 yok); arama aynı JOIN üzerinden çalışır.
        var query = _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Customer)
            .Include(policy => policy.Quote)
            .Where(policy => policy.StartDate >= fromInclusive && policy.StartDate <= toInclusive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumberSearch.ToSubscriberDigits(term);
            var hasPhone = phone.Length >= PhoneNumberSearch.MinSubscriberDigits;

            // Müşteri ad/soyad/tam ad, telefon (kanonik "+90…" → "+" atılıp abone son eki) veya poliçe numarası.
            query = query.Where(policy =>
                policy.PolicyNumber.Contains(term)
                || (policy.Customer!.FirstName + " " + policy.Customer.LastName).Contains(term)
                || policy.Customer.FirstName.Contains(term)
                || policy.Customer.LastName.Contains(term)
                || (hasPhone && policy.Customer.PhoneNumber.Replace("+", "").Contains(phone)));
        }

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
