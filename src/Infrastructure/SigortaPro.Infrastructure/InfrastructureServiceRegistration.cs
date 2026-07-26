using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Infrastructure.BackgroundJobs;
using SigortaPro.Infrastructure.Email;
using SigortaPro.Infrastructure.RealTime;
using SigortaPro.Infrastructure.Security;
using SigortaPro.Infrastructure.Services;
using SigortaPro.Infrastructure.Services.CityCatalog;
using SigortaPro.Infrastructure.Services.Documents;
using SigortaPro.Infrastructure.Services.EarthquakeZone;
using SigortaPro.Infrastructure.Services.Email;
using SigortaPro.Infrastructure.Services.Payment;
using SigortaPro.Infrastructure.Services.Pricing;
using SigortaPro.Infrastructure.Services.Storage;
using SigortaPro.Infrastructure.Services.VehicleCatalog;

namespace SigortaPro.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("'JwtSettings' yapılandırması appsettings.json içinde tanımlı değil.");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("'JwtSettings:SecretKey' değeri tanımlı değil.");
        }

        services.AddSingleton(jwtSettings);
        services.AddScoped<ITokenService, TokenService>();

        // IDateTimeProvider implementasyonu (ARCHITECTURE_RULES.md §6.2: Singleton, stateless).
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // ADR-008: Kural tabanlı mock fiyatlama motoru (ARCHITECTURE_RULES.md §6.2: Scoped).
        services.AddScoped<IPricingEngine, PricingEngine>();

        // ADR-049: Yerleşik baz tarife sağlayıcısı — motorla aynı sabit değerleri açar (stateless → Singleton).
        services.AddSingleton<IPricingBaselineProvider, PricingBaselineProvider>();

        // ADR-007: Mock sanal POS ödeme servisi (ARCHITECTURE_RULES.md §6.2: Scoped).
        services.AddScoped<IPaymentService, MockVirtualPosService>();

        // ADR-006/ADR-023: QuestPDF Community lisansı (ücretsiz) süreç genelinde bir kez ayarlanır.
        QuestPDF.Settings.License = LicenseType.Community;

        // PDF render (saf) ve dosya saklama (yerel disk) — stateless olduklarından Singleton (ARCHITECTURE_RULES.md §6.2).
        services.AddSingleton<IPolicyDocumentService, PolicyPdfDocumentService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Task 13: mock bildirim servisi (log/e-posta simülasyonu — ARCHITECTURE_RULES.md §6.1).
        services.AddScoped<INotificationService, MockNotificationService>();

        // ADR-035: E-posta altyapısı. EmailSettings appsettings + user-secrets/ortam değişkeninden bağlanır
        // (bölüm yoksa boş varsayılan — fail-fast YOK: SMTP yapılandırılmamışsa gönderim EmailDeliveryException
        // fırlatır ve şifre sıfırlama akışı bunu sızdırmadan ele alır). IEmailService genel transport (SMTP);
        // PasswordResetNotifier link/şablon kurup transport'u kullanır (sağlayıcı geçişinde yalnızca transport değişir).
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        services.AddSingleton(emailSettings);
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IPasswordResetNotifier, PasswordResetNotifier>();

        // Task 13: poliçe yaşam döngüsü arkaplan servisi (teklif/poliçe expiry + yenileme teklifi üretimi).
        services.AddHostedService<PolicyLifecycleBackgroundService>();

        // ADR-036: Araç kataloğu gömülü JSON'dan bir defa okunup In-Memory cache'lenir → Singleton.
        services.AddSingleton<IVehicleCatalogProvider, VehicleCatalogProvider>();

        // ADR-037: İl kataloğu (81 il) gömülü JSON'dan bir defa okunup In-Memory cache'lenir → Singleton.
        services.AddSingleton<ICityCatalogProvider, CityCatalogProvider>();

        // ADR-055: Deprem bölgesi il'den türetilir (kullanıcı beyanı değil) — aynı gömülü JSON deseni.
        services.AddSingleton<IEarthquakeZoneProvider, EarthquakeZoneProvider>();

        // ADR-041: Gerçek zamanlı bildirim altyapısı (SignalR). Hub uç noktası WebAPI'de map edilir
        // (composition root); yayın soyutlaması taşıyıcıdan bağımsızdır (IRealTimeNotifier).
        services.AddSignalR();
        services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

        return services;
    }
}
