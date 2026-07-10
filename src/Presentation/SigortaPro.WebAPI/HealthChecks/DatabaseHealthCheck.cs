using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SigortaPro.Persistence.Context;

namespace SigortaPro.WebAPI.HealthChecks;

// Veritabanı bağlantısının sağlığını kontrol eder. Ek NuGet paketi (HealthChecks.EntityFrameworkCore) yerine
// EF Core'un CanConnectAsync metodu üzerinden hafif bir kontrol yapar (ASP.NET Core shared framework yeterlidir).
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Veritabanı bağlantısı sağlıklı.")
                : HealthCheckResult.Unhealthy("Veritabanına bağlanılamıyor.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Veritabanı sağlık kontrolü başarısız.", exception);
        }
    }
}
