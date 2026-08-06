namespace SigortaPro.Application.Common.Interfaces;

// Şifre sıfırlama e-postasını oluşturup gönderir (link kurulumu + şablon Infrastructure'da;
// taban URL EmailSettings'ten okunur). INotificationService/MockNotificationService deseninin
// izidir: Application yalnızca "bu kullanıcıya sıfırlama linkini gönder" niyetini bilir, taşıma/şablon
// detayları Infrastructure'da kalır. Böylece IEmailService genel/soyut transport olarak sağlayıcıdan bağımsız kalır.
public interface IPasswordResetNotifier
{
    // Verilen ham reset token'ı ile URL-güvenli bir sıfırlama linki kurup e-posta gönderir.
    // Token/link asla loglanmaz. Gönderim başarısız olursa EmailDeliveryException fırlatır.
    Task SendResetLinkAsync(string email, string resetToken, CancellationToken cancellationToken = default);
}
