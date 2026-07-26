using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// ADR-048: IPricingVersionRepository implementasyonu. Okuma sorguları AsNoTracking + Include(Rates);
// versiyonlar değişmez olduğundan güncelleme yolu yoktur.
public sealed class PricingVersionRepository : GenericRepository<PricingVersion>, IPricingVersionRepository
{
    private readonly AppDbContext _context;

    public PricingVersionRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<PricingVersion?> GetEffectiveAsync(DateTime asOf, CancellationToken cancellationToken = default) =>
        _context.PricingVersions
            .AsNoTracking()
            .Include(version => version.Rates)
            .Where(version => version.EffectiveFrom <= asOf)
            .OrderByDescending(version => version.EffectiveFrom)
            .ThenByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

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
        var maxVersionNumber = await _context.PricingVersions
            .AsNoTracking()
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken);

        return (maxVersionNumber ?? 0) + 1;
    }
}
