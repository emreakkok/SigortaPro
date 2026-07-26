using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Infrastructure.Email;

namespace SigortaPro.Infrastructure.Services.Email;

// IEmailService'in MVP SMTP implementasyonu (ADR-035). MailKit kullanılır (System.Net.Mail.SmtpClient
// obsolete olduğu için tercih edilmez). Sağlayıcıya özgü hatalar EmailDeliveryException'a sarılır; böylece
// Application katmanı MailKit'e bağımlı olmadan hatayı tiplenmiş yakalayabilir. E-posta gövdesi, alıcı token'ı
// veya SMTP parolası ASLA loglanmaz (CLAUDE.md §4.5, §8.3).
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(EmailSettings settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.ToEmail));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            bodyBuilder.TextBody = message.PlainTextBody;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        var socketOptions = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            // Konu dışında hiçbir hassas içerik (alıcı/link/token) loglanmaz.
            _logger.LogInformation("E-posta SMTP ile gönderildi. Konu: {Subject}", message.Subject);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Sağlayıcıya özgü (MailKit) hataları tiplenmiş EmailDeliveryException'a sararız; çağıran akış
            // (ör. şifre sıfırlama) bunu güvenli biçimde ele alır. Detay loglanır ancak hassas veri içermez.
            throw new EmailDeliveryException("E-posta gönderimi başarısız oldu.", exception);
        }
    }
}
