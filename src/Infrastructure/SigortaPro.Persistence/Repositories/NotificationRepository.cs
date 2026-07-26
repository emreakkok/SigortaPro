using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Repositories;

// INotificationRepository implementasyonu (ADR-042). Liste sorguları AsNoTracking + sayfalıdır;
// okundu işaretleme yolları tracked çalışır (domain MarkAsRead metodu üzerinden).
public sealed class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<PagedResult<Notification>> GetPagedForRecipientAsync(
        Guid recipientUserId,
        bool? isRead,
        string? severity,
        string? searchTerm,
        DateTime? from,
        DateTime? to,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId);

        if (isRead is not null)
        {
            query = isRead.Value
                ? query.Where(notification => notification.ReadAt != null)
                : query.Where(notification => notification.ReadAt == null);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(notification => notification.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // ADR-047: arama actor adı ve operasyonel referansı (ör. poliçe numarası) da kapsar —
            // personel "POL-2026-000123" veya "Ahmet" yazarak ilgili olayları bulabilir.
            var term = searchTerm.Trim();
            query = query.Where(notification =>
                notification.Title.Contains(term)
                || notification.Message.Contains(term)
                || (notification.ActorName != null && notification.ActorName.Contains(term))
                || (notification.ReferenceCode != null && notification.ReferenceCode.Contains(term)));
        }

        if (from is not null)
        {
            query = query.Where(notification => notification.CreatedAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(notification => notification.CreatedAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Notification>(items, paging.Page, paging.PageSize, totalCount);
    }

    public Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
        _context.Notifications
            .AsNoTracking()
            .CountAsync(
                notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null,
                cancellationToken);

    public Task<Notification?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetUnreadTrackedAsync(
        Guid recipientUserId, CancellationToken cancellationToken = default) =>
        await _context.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null)
            .ToListAsync(cancellationToken);
}
