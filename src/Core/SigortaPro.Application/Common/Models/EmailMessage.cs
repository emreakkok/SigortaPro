namespace SigortaPro.Application.Common.Models;

// Sağlayıcıdan bağımsız (SMTP/SendGrid/Mailgun) tek bir e-posta gönderim isteğini temsil eder.
// İçerik (konu/gövde) Application katmanında oluşturulur; taşıma (transport) Infrastructure'dadır.
public sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody, string? PlainTextBody = null);
