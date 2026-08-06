using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Bildirim modülüne özgü sorgular.
// Tüm okuma yüzeyi alıcı (RecipientUserId) kapsamlıdır — kullanıcı yalnızca kendi bildirimlerini görür.
public interface INotificationRepository : IReadRepository<Notification>, IWriteRepository<Notification>
{
    // Bildirim merkezi listesi: en yeni önce; okunma durumu / önem / tür / metin araması / tarih filtreli.
    Task<PagedResult<Notification>> GetPagedForRecipientAsync(
        Guid recipientUserId,
        bool? isRead,
        string? severity,
        string? searchTerm,
        DateTime? from,
        DateTime? to,
        PaginationParams paging,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    // Okundu işaretleme için izlemeli (tracked) tekil kayıt.
    Task<Notification?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // "Tümünü okundu işaretle" için alıcının okunmamış kayıtları (tracked).
    Task<IReadOnlyList<Notification>> GetUnreadTrackedAsync(Guid recipientUserId, CancellationToken cancellationToken = default);
}
