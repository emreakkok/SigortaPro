using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.WebAPI.Tests.Integration;

// Test ortamı için no-op IEmailService (Fake/Null). Entegrasyon testlerinde gerçek SmtpEmailService yerine
// bu implementasyon DI'a enjekte edilir; böylece hiçbir otomatik test gerçek SMTP'ye bağlanmaz veya internete
// çıkmaz — rastgele/gerçek e-posta adreslerine mail gönderilmesi tamamen imkânsızdır (kullanıcı gereksinimi).
// Not: Gerçek SMTP yalnızca Development ortamında, user-secrets ile yapılandırıldığında ve manuel test
// sırasında çalışır; test host'u "Testing" ortamında bu no-op'u kullanır.
internal sealed class NullEmailService : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
