using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IPolicyRepository implementasyonu.
public sealed class PolicyRepository : GenericRepository<Policy>, IPolicyRepository
{
    private readonly AppDbContext _context;

    public PolicyRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    // Yıla ait poliçe sayısı (sıralı numara üretimi). Soft-delete edilmiş poliçeler de benzersiz
    // numarayı işgal ettiğinden numara tekrarını önlemek için query filter yok sayılır (IgnoreQueryFilters).
    public Task<int> CountByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"{BusinessConstants.PolicyNumberPrefix}-{year}-";

        return _context.Policies
            .IgnoreQueryFilters()
            .CountAsync(policy => policy.PolicyNumber.StartsWith(prefix), cancellationToken);
    }

    // Salt okunur, sayfalanmış "Poliçelerim" listesi: müşteri filtresi + opsiyonel durum; branş/ürün
    // adı için teklif+ürün yüklenir. En yeni başlangıç önce (kararlı sıralama için Id ikincil).
    public async Task<PagedResult<Policy>> GetByCustomerPagedAsync(
        Guid customerId,
        PolicyStatus? status,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.InsuranceProduct)
            .Where(policy => policy.CustomerId == customerId);

        if (status is not null)
        {
            query = query.Where(policy => policy.Status == status);
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

    // Salt okunur (AsNoTracking) detay: müşteri, teklif (ürün+teminatlar, araç/konut) ile — teminat tablosunun
    // deterministik yeniden hesabı için (GetPolicyById). Belge yazımı gerekmediğinden tracked değildir.
    public Task<Policy?> GetReadDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Policies
            .AsNoTracking()
            .Include(policy => policy.Customer)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.InsuranceProduct!).ThenInclude(product => product.Coverages)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Vehicle)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Property)
            .FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    // İzlemeli detay (PDF üretiminde belge eklenip kaydedilebilir): müşteri, teklif (ürün+teminatlar,
    // araç/konut) ve varsa mevcut belge yüklenir.
    public Task<Policy?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Policies
            .Include(policy => policy.Customer)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.InsuranceProduct!).ThenInclude(product => product.Coverages)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Vehicle)
            .Include(policy => policy.Quote!).ThenInclude(quote => quote.Property)
            .Include(policy => policy.PolicyDocument)
            .FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    // İzlemeli: bitiş tarihi geçmiş aktif poliçeler (arkaplan expiry —).
    public async Task<IReadOnlyList<Policy>> GetOverdueActiveAsync(DateTime asOf, CancellationToken cancellationToken = default) =>
        await _context.Policies
            .Where(policy => policy.Status == PolicyStatus.Active && policy.EndDate < asOf)
            .ToListAsync(cancellationToken);

    // Salt okunur: bitişine ≤ pencere kadar kalan, henüz yenileme teklifi olmayan aktif poliçeler; özgün teklif
    // (ürün+teminatlar, risk objesi) ve müşteri ile birlikte (yenileme fiyatlaması için,).
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

    // Bonus-Malus girdisi. Tek sorgu; SQL tarafında COUNT + NOT EXISTS ile hesaplanır (N+1 yok).
    // Branş, poliçenin kaynaklandığı teklifden okunur → Kasko ve Trafik AYRI basamak taşır.
    public Task<int> CountClaimFreeCompletedPeriodsAsync(
        Guid customerId, InsuranceBranch branch, DateTime asOf, CancellationToken cancellationToken = default) =>
        _context.Policies
            .AsNoTracking()
            .CountAsync(
                policy => policy.CustomerId == customerId
                    && policy.Quote!.Branch == branch
                    && policy.EndDate < asOf
                    && policy.Status != PolicyStatus.Cancelled
                    && !policy.Claims.Any(claim =>
                        claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.Paid),
                cancellationToken);
}
