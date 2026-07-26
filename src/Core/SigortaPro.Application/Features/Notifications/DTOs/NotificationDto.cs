namespace SigortaPro.Application.Features.Notifications.DTOs;

// Kalıcı bildirim görünümü (ADR-042). Severity: "success" | "info" | "warning" | "error".
// ADR-047 (additive, sona eklendi → mevcut sözleşme kırılmaz): ActorName = işlemi yapanın o andaki
// görünen adı (snapshot), ReferenceCode = operasyonel referans (ör. poliçe numarası).
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Severity,
    string Title,
    string Message,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt,
    string? ActorName = null,
    string? ReferenceCode = null);
