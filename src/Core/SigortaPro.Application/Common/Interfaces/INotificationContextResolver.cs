namespace SigortaPro.Application.Common.Interfaces;

// ADR-047: Bildirim kataloğunun "kim yaptı / kimin için" bağlamını çözen yardımcı servis.
// Yeni bir audit sistemi DEĞİLDİR — yalnızca mevcut Identity/Customer yapılarını yeniden kullanır.
public interface INotificationContextResolver
{
    // İşlemi yapan kullanıcı. Personelde e-posta (iç operasyon kimliği), müşteride ad-soyad döner;
    // anonim akışlarda (şifre sıfırlama) boş actor döner.
    Task<NotificationActor> ResolveActorAsync(CancellationToken cancellationToken = default);

    // Müşteri görünen adı (Customer.Id üzerinden). Bulunamazsa null.
    Task<string?> GetCustomerNameAsync(Guid customerId, CancellationToken cancellationToken = default);
}

// İşlemi yapan kullanıcının bildirim anındaki görünümü. IsStaff, "personel bir müşteri adına işlem
// yaptı" ayrımını kurmak için taşınır (mesaj metni buna göre kurulur).
public sealed record NotificationActor(Guid? UserId, string? DisplayName, bool IsStaff)
{
    public static readonly NotificationActor Anonymous = new(null, null, false);
}
