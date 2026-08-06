using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// IInsuranceProductRepository implementasyonu.
public sealed class InsuranceProductRepository : GenericRepository<InsuranceProduct>, IInsuranceProductRepository
{
    private readonly AppDbContext _context;

    public InsuranceProductRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<InsuranceProduct?> GetActiveByBranchAsync(InsuranceBranch branch, CancellationToken cancellationToken = default) =>
        _context.InsuranceProducts
            .AsNoTracking()
            .Include(product => product.Coverages)
            .FirstOrDefaultAsync(product => product.Branch == branch && product.IsActive, cancellationToken);
}
