using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Common;

namespace SigortaPro.Persistence.Interceptors;

// ARCHITECTURE_RULES.md §7.2: CreatedAt/UpdatedAt/CreatedBy/UpdatedBy SaveChanges interceptor'ı ile
// otomatik doldurulur. ICurrentUserService (impl. WebAPI — Task 5/6) ve IDateTimeProvider (impl.
// Infrastructure — Task 6) henüz implement edilmediğinden parametreler opsiyoneldir: .NET DI container'ı
// kayıtlı olmayan bir servis için varsayılan (null) değere düşer. Bu sayede Task 4 kendi başına
// çalışabilir; gerçek implementasyonlar eklendiğinde bu sınıfta hiçbir değişiklik gerekmez.
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly ICurrentUserService? _currentUserService;

    public AuditableEntityInterceptor(
        IDateTimeProvider? dateTimeProvider = null,
        ICurrentUserService? currentUserService = null)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var userId = _currentUserService?.UserId?.ToString();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
