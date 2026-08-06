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

        // IDateTimeProvider implementasyonu.
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Kural tabanlı mock fiyatlama motoru.
        services.AddScoped<IPricingEngine, PricingEngine>();

        // Yerleşik baz tarife sağlayıcısı — motorla aynı sabit değerleri açar (stateless → Singleton).
        services.AddSingleton<IPricingBaselineProvider, PricingBaselineProvider>();

        // Mock sanal POS ödeme servisi.
        services.AddScoped<IPaymentService, MockVirtualPosService>();

        // QuestPDF Community lisansı (ücretsiz) süreç genelinde bir kez ayarlanır.
        QuestPDF.Settings.License = LicenseType.Community;

        // PDF render (saf) ve dosya saklama (yerel disk) — stateless olduklarından Singleton.
        services.AddSingleton<IPolicyDocumentService, PolicyPdfDocumentService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // mock bildirim servisi (log/e-posta simülasyonu).
        services.AddScoped<INotificationService, MockNotificationService>();

        // E-posta altyapısı. EmailSettings appsettings + user-secrets/ortam değişkeninden bağlanır
        // (bölüm yoksa boş varsayılan — fail-fast YOK: SMTP yapılandırılmamışsa gönderim EmailDeliveryException
        // fırlatır ve şifre sıfırlama akışı bunu sızdırmadan ele alır). IEmailService genel transport (SMTP);
        // PasswordResetNotifier link/şablon kurup transport'u kullanır (sağlayıcı geçişinde yalnızca transport değişir).
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        services.AddSingleton(emailSettings);
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IPasswordResetNotifier, PasswordResetNotifier>();

        // poliçe yaşam döngüsü arkaplan servisi (teklif/poliçe expiry + yenileme teklifi üretimi).
        services.AddHostedService<PolicyLifecycleBackgroundService>();

        // Araç kataloğu gömülü JSON'dan bir defa okunup In-Memory cache'lenir → Singleton.
        services.AddSingleton<IVehicleCatalogProvider, VehicleCatalogProvider>();

        // İl kataloğu (81 il) gömülü JSON'dan bir defa okunup In-Memory cache'lenir → Singleton.
        services.AddSingleton<ICityCatalogProvider, CityCatalogProvider>();

        // Deprem bölgesi il'den türetilir (kullanıcı beyanı değil) — aynı gömülü JSON deseni.
        services.AddSingleton<IEarthquakeZoneProvider, EarthquakeZoneProvider>();

        // Gerçek zamanlı bildirim altyapısı (SignalR). Hub uç noktası WebAPI'de map edilir
        // (composition root); yayın soyutlaması taşıyıcıdan bağımsızdır (IRealTimeNotifier).
        services.AddSignalR();
        services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

        return services;
    }
}
