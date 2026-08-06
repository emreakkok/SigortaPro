namespace SigortaPro.Application.Common.Notifications;

// Gerçek zamanlı bildirim önem düzeyi (frontend toast/rozet varyantlarıyla birebir).
public static class NotificationSeverity
{
    public const string Success = "success";
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
}

// Taşıyıcıdan bağımsız gerçek zamanlı bildirim sözleşmesi. Hassas veri (TCKN, e-posta,
// kart, şifre/token) TAŞIMAZ — yalnızca olay türü + kullanıcıya gösterilecek Türkçe başlık/mesaj.
// Type, frontend'in cache invalidation/yönlendirme eşlemesi için makine-okur kimliktir (ör. "quote-created").
// RelatedEntityId/Type: bildirimin işaret ettiği kayıt (ör. Policy/Claim) —
// bildirim merkezinde tıkla-git navigasyonuna ve kalıcı kayda taşınır; hassas veri değildir.
// ActorUserId/ActorName: işlemi yapan kullanıcı — ad, oluşturma anındaki snapshot'tır.
// ReferenceCode: operasyonel referans (ör. poliçe numarası); karşılığı yoksa null.
public sealed record RealTimeNotification(
    string Type,
    string Severity,
    string Title,
    string Message,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null,
    Guid? ActorUserId = null,
    string? ActorName = null,
    string? ReferenceCode = null);
