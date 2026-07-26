using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Infrastructure.Email;

namespace SigortaPro.Infrastructure.Services.Email;

// IPasswordResetNotifier implementasyonu (ADR-035): reset linkini EmailSettings:ResetPasswordBaseUrl üzerinden
// URL-güvenli biçimde kurar, Türkçe e-posta şablonunu oluşturur ve genel IEmailService transport'u ile gönderir.
// MockNotificationService deseninin izidir (mesajı Infrastructure oluşturur). Link/token loglanmaz.
public sealed class PasswordResetNotifier : IPasswordResetNotifier
{
    private readonly EmailSettings _settings;
    private readonly IEmailService _emailService;

    public PasswordResetNotifier(EmailSettings settings, IEmailService emailService)
    {
        _settings = settings;
        _emailService = emailService;
    }

    public Task SendResetLinkAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var resetLink = BuildResetLink(email, resetToken);

        const string subject = "SigortaPro — Şifre Sıfırlama Talebi";
        // ADR-040: kurumsal palet (bordo #7A1F2B, koyu gri, kırık beyaz) — web/PDF ile aynı kimlik.
        // Yalnızca kozmetik inline stil; gönderim davranışı değişmez.
        var htmlBody =
            $"""
            <div style="background:#F7F5F2;padding:24px;font-family:Arial,Helvetica,sans-serif;color:#26262B;">
              <div style="max-width:480px;margin:0 auto;background:#FFFFFF;border:1px solid #DCDAD5;border-radius:8px;padding:24px;">
                <p style="margin:0 0 8px;font-size:18px;font-weight:bold;color:#7A1F2B;">SigortaPro</p>
                <p>Merhaba,</p>
                <p>Hesabınız için bir şifre sıfırlama talebi aldık. Yeni şifrenizi belirlemek için aşağıdaki bağlantıya tıklayın:</p>
                <p style="text-align:center;margin:24px 0;">
                  <a href="{resetLink}" style="background:#7A1F2B;color:#F7F5F2;padding:10px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">Şifremi Sıfırla</a>
                </p>
                <p>Bu bağlantı <strong>1 saat</strong> boyunca geçerlidir. Talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz; şifreniz değişmez.</p>
                <p style="color:#6E6E76;font-size:12px;margin-top:24px;">SigortaPro — bu e-posta otomatik gönderilmiştir.</p>
              </div>
            </div>
            """;
        var plainTextBody =
            $"""
            Merhaba,

            Hesabınız için bir şifre sıfırlama talebi aldık. Yeni şifrenizi belirlemek için aşağıdaki bağlantıyı kullanın:
            {resetLink}

            Bu bağlantı 1 saat boyunca geçerlidir. Talebi siz yapmadıysanız bu e-postayı yok sayabilirsiniz; şifreniz değişmez.

            SigortaPro
            """;

        return _emailService.SendAsync(new EmailMessage(email, subject, htmlBody, plainTextBody), cancellationToken);
    }

    private string BuildResetLink(string email, string resetToken)
    {
        // Token DataProtector çıktısıdır ve '+', '/', '=' içerebilir; e-posta da '@' içerir → URL-encode zorunlu.
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(resetToken);
        var baseUrl = _settings.ResetPasswordBaseUrl.TrimEnd('/');

        return $"{baseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";
    }
}
