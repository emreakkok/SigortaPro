using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IPricingVersionRepository implementasyonu. Okuma sorguları AsNoTracking + Include(Rates);
// yalnızca TASLAK düzenlenir (izlemeli getirilir), aktif/arşiv değişmez.
public sealed class PricingVersionRepository : GenericRepository<PricingVersion>, IPricingVersionRepository
{
    private readonly AppDbContext _context;

    public PricingVersionRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<PricingVersion?> GetActiveAsync(CancellationToken cancellationToken = default) =>
        _context.PricingVersions
            .AsNoTracking()
            .Include(version => version.Rates)
            .FirstOrDefaultAsync(version => version.Status == PricingVersionStatus.Active, cancellationToken);

    public Task<PricingVersion?> GetDraftAsync(CancellationToken cancellationToken = default) =>
        _context.PricingVersions
            .AsNoTracking()
            .Include(version => version.Rates)
            .FirstOrDefaultAsync(version => version.Status == PricingVersionStatus.Draft, cancellationToken);

    public Task<PricingVersion?> GetTrackedWithRatesByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PricingVersions
            .Include(version => version.Rates)
            .FirstOrDefaultAsync(version => version.Id == id, cancellationToken);

    public Task<PricingVersion?> GetWithRatesByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PricingVersions
            .AsNoTracking()
            .Include(version => version.Rates)
            .FirstOrDefaultAsync(version => version.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PricingVersion>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
        await _context.PricingVersions
            .AsNoTracking()
            .Include(version => version.Rates)
            .OrderByDescending(version => version.EffectiveFrom)
            .ThenByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextVersionNumberAsync(CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: soft-delete edilmiş (iptal edilmiş taslak) satırlar da sayılır → versiyon
        // numarası GLOBAL MONOTONIK kalır ve VersionNumber benzersiz indeksiyle asla çakışmaz (iptal edilen
        // bir taslağın numarası yeniden kullanılmaz).
        var maxVersionNumber = await _context.PricingVersions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken);

        return (maxVersionNumber ?? 0) + 1;
    }
}
