using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

// Kalıcı kullanıcı bildirimi. Alıcı bazlı satır modeli: bir olay, hedef kitledeki her
// kullanıcı için ayrı kayıt üretir (okundu durumu kullanıcıya özeldir). RecipientUserId, Identity
// kullanıcısına (AppUserId) işaret eden düz Guid'dir — Domain, Identity tiplerini bilmez.
public class Notification : BaseEntity, IAggregateRoot
{
    protected Notification()
    {
        Type = string.Empty;
        Severity = string.Empty;
        Title = string.Empty;
        Message = string.Empty;
    }

    public Notification(
        Guid recipientUserId,
        string type,
        string severity,
        string title,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        Guid? actorUserId = null,
        string? actorName = null,
        string? referenceCode = null)
    {
        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        Type = type;
        Severity = severity;
        Title = title;
        Message = message;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        ActorUserId = actorUserId;
        ActorName = actorName;
        ReferenceCode = referenceCode;
    }

    public Guid RecipientUserId { get; private set; }

    // Makine-okur olay türü (ör. "quote-created") — frontend eşleme/filtreleme anahtarı.
    public string Type { get; private set; }

    // "success" | "info" | "warning" | "error" (NotificationSeverity sabitleri).
    public string Severity { get; private set; }

    public string Title { get; private set; }
    public string Message { get; private set; }

    // Bildirimin işaret ettiği kayıt (ör. poliçe/hasar) — tıkla-git navigasyonu için.
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }

    // (additive): "işlemi kim yaptı" bağlamı. ActorUserId stabil kimlik referansıdır;
    // ActorName bildirim oluşturulduğu andaki görünen ad **snapshot**'ıdır — kullanıcı adını sonradan
    // değiştirse bile geçmiş bildirim o anki gerçeği gösterir (activity feed girdisi geçmişe aittir).
    // Anonim akışlarda (şifre sıfırlama) her ikisi de null'dır.
    public Guid? ActorUserId { get; private set; }
    public string? ActorName { get; private set; }

    // Operasyonel arama/eşleştirme anahtarı (ör. poliçe numarası). Veri modelinde karşılığı olmayan
    // kayıtlarda (teklif/hasar numarası yoktur) null bırakılır — uydurma kod üretilmez.
    public string? ReferenceCode { get; private set; }

    public DateTime? ReadAt { get; private set; }
    public bool IsRead => ReadAt is not null;

    /// <summary>Bildirimi okundu işaretler; tekrar çağrılar ilk okuma zamanını korur (idempotent).</summary>
    public void MarkAsRead(DateTime readAt)
    {
        ReadAt ??= readAt;
    }
}
