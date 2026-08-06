using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Notifications;

// INotificationDispatcher implementasyonu (Application orkestrasyonu — iş kuralı içermez,
// yalnızca kalıcılık + canlı yayın koordinasyonu). Fan-out: staff (Admin ∪ Personel) kullanıcılarının
// her biri için ayrı Notification satırı yazılır (okundu durumu kullanıcıya özel), ardından "staff"
// grubuna tek canlı yayın yapılır. Çağıran (RealTimeNotificationBehavior) hataları yan-kanal olarak ele alır.
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IIdentityService _identityService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealTimeNotifier _realTimeNotifier;

    public NotificationDispatcher(
        IIdentityService identityService,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IRealTimeNotifier realTimeNotifier)
    {
        _identityService = identityService;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _realTimeNotifier = realTimeNotifier;
    }

    public async Task PublishToStaffAsync(RealTimeNotification notification, CancellationToken cancellationToken = default)
    {
        // staff kümesi (Admin ∪ Personel) tek kaynaktan (Roles.StaffRoles) türetilir.
        var recipientIds = new List<Guid>();
        foreach (var role in Roles.StaffRoles)
        {
            recipientIds.AddRange(await _identityService.GetUserIdsInRoleAsync(role, cancellationToken));
        }

        recipientIds = recipientIds.Distinct().ToList();

        foreach (var recipientId in recipientIds)
        {
            await _notificationRepository.AddAsync(
                new Notification(
                    recipientId,
                    notification.Type,
                    notification.Severity,
                    notification.Title,
                    notification.Message,
                    notification.RelatedEntityId,
                    notification.RelatedEntityType,
                    // actor snapshot'ı + operasyonel referans kalıcı kayda taşınır.
                    notification.ActorUserId,
                    notification.ActorName,
                    notification.ReferenceCode),
                cancellationToken);
        }

        if (recipientIds.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Canlı yayın kalıcılıktan sonra yapılır: istemci invalidation'ı sunucudan güncel listeyi çeker.
        await _realTimeNotifier.NotifyStaffAsync(notification, cancellationToken);
    }
}
