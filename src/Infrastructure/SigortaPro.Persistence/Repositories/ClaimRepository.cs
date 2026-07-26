using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IClaimRepository implementasyonu (ADR-005, ARCHITECTURE_RULES.md §4.2).
public sealed class ClaimRepository : GenericRepository<Claim>, IClaimRepository
{
    private readonly AppDbContext _context;

    public ClaimRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    // Salt okunur detay: poliçesiyle (poliçe numarası gösterimi için) ve eklenen belgeleriyle (foto/PDF).
    public Task<Claim?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Claims
            .AsNoTracking()
            .Include(claim => claim.Policy)
            .Include(claim => claim.Documents)
            .FirstOrDefaultAsync(claim => claim.Id == id, cancellationToken);

    // Durum geçişi komutları için izlemeli hasar; özet yanıtta poliçe numarası için poliçe yüklenir.
    public Task<Claim?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Claims
            .Include(claim => claim.Policy)
            .FirstOrDefaultAsync(claim => claim.Id == id, cancellationToken);

    public async Task<PagedResult<Claim>> SearchAsync(
        Guid? customerId,
        ClaimStatus? status,
        Guid? policyId,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Claims
            .AsNoTracking()
            .Include(claim => claim.Policy)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(claim => claim.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(claim => claim.Status == status.Value);
        }

        if (policyId.HasValue)
        {
            query = query.Where(claim => claim.PolicyId == policyId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(claim => claim.CreatedAt)
            .ThenBy(claim => claim.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, paging.Page, paging.PageSize, totalCount);
    }

    // Fiyatlamaya etki eden hasar geçmişi: onaylanmış + ödenmiş hasar sayısı (Task 13 yenileme fiyatlaması besler).
    // ADR-054: Hasar geçmişi BRANŞ KAPSAMLI sayılır (Policy → Quote.Branch join'i ile). Önceden müşterinin
    // tüm branşlardaki hasarları sayılıyordu; bu, bir Kasko hasarının Sağlık yenileme primini artırmasına
    // yol açıyordu (branşlar arası risk kirlenmesi).
    public Task<int> CountReportableClaimsByCustomerAsync(
        Guid customerId, InsuranceBranch branch, CancellationToken cancellationToken = default) =>
        _context.Claims
            .CountAsync(
                claim => claim.CustomerId == customerId
                    && (claim.Status == ClaimStatus.Approved || claim.Status == ClaimStatus.Paid)
                    && claim.Policy!.Quote!.Branch == branch,
                cancellationToken);
}
