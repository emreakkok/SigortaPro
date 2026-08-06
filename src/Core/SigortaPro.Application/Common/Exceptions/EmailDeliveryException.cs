namespace SigortaPro.Application.Common.Exceptions;

// E-posta gönderimi (SMTP bağlantısı/kimlik doğrulama/gönderim) başarısız olduğunda fırlatılır.
// Infrastructure'daki e-posta implementasyonu, sağlayıcıya özgü (ör. MailKit) hataları bu tipe sarar;
// böylece Application katmanı sağlayıcı istisnalarına bağımlı olmadan hatayı tiplenmiş şekilde yakalayabilir
// . SigortaProException hiyerarşisinin parçası DEĞİLDİR: bir e-posta
// gönderim hatası, kullanıcıya döndürülecek bir iş kuralı/doğrulama hatası değildir; çağıran akış (ör. şifre
// sıfırlama, güvenlik gereği) bunu kullanıcıya sızdırmadan ele alır.
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
