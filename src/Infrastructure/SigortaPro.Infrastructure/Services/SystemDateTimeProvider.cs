using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Infrastructure.Services;

// IDateTimeProvider implementasyonu (ARCHITECTURE_RULES.md §6.1, ADR-016). Sistem saatini soyutlar;
// handler'ların test edilebilir olmasını ve audit interceptor'ının gerçek zaman kaynağını kullanmasını sağlar.
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
