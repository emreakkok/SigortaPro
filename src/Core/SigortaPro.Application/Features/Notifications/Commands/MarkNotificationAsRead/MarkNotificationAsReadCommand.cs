using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Notifications.Commands.MarkNotificationAsRead;

// Tek bildirimi okundu işaretler (idempotent). Yalnızca alıcısı işaretleyebilir (sahiplik → 403).
public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
