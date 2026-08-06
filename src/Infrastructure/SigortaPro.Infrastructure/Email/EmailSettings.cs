namespace SigortaPro.Infrastructure.Email;

// SMTP e-posta gönderimi ve şifre sıfırlama linki için ortak ayarlar (JwtSettings deseni).
// appsettings.json "EmailSettings" bölümünden bağlanır. Gerçek kullanıcı adı/parola koda veya
// appsettings dosyalarına YAZILMAZ; geliştirmede `dotnet user-secrets`, üretimde ortam değişkeni/secret store
// ile sağlanır. Parola hiçbir log kaydına yazılmaz.
public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;

    // true → STARTTLS (587), false → SSL-on-connect (465).
    public bool UseStartTls { get; init; } = true;

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "SigortaPro";

    // Şifre sıfırlama linkinin işaret edeceği SPA taban adresi (ör. http://localhost:5173).
    // Link "{ResetPasswordBaseUrl}/reset-password?email=...&token=..." biçiminde kurulur.
    public string ResetPasswordBaseUrl { get; init; } = string.Empty;
}
