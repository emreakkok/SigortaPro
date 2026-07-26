using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Application.Common.Interfaces;

// ADR-042: Bildirim yayın orkestrasyonu — tek çağrıda (1) kalıcı kayıt (alıcı başına fan-out) ve
// (2) SignalR canlı yayın (IRealTimeNotifier). Behavior yalnızca bu soyutlamayı çağırır; kalıcılık ve
// taşıyıcı detayları arkada kalır. İleride e-posta/mobil push kanalları bu orkestrasyona eklenebilir.
public interface INotificationDispatcher
{
    // Acente personeline (Admin ∪ Personel): her staff kullanıcısı için kalıcı kayıt + "staff" grubuna canlı yayın.
    Task PublishToStaffAsync(RealTimeNotification notification, CancellationToken cancellationToken = default);
}
