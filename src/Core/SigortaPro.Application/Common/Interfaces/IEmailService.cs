using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Common.Interfaces;

// Sağlayıcıdan bağımsız e-posta gönderim soyutlaması — arayüz Application'da,
// implementasyon Infrastructure'da). MVP'de SMTP tabanlı SmtpEmailService kullanılır; ileride
// SendGrid/Mailgun gibi transactional sağlayıcılara geçiş yalnızca yeni bir implementasyon eklemekle mümkündür.
// Gönderim başarısız olursa EmailDeliveryException fırlatır; çağıran katman bu hatayı ele alır.
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
