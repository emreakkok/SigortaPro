using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Infrastructure.BackgroundJobs;
using SigortaPro.Infrastructure.Security;
using SigortaPro.Infrastructure.Services;
using SigortaPro.Infrastructure.Services.Documents;
using SigortaPro.Infrastructure.Services.Payment;
using SigortaPro.Infrastructure.Services.Pricing;
using SigortaPro.Infrastructure.Services.Storage;

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

        // ADR-007: Mock sanal POS ödeme servisi (ARCHITECTURE_RULES.md §6.2: Scoped).
        services.AddScoped<IPaymentService, MockVirtualPosService>();

        // ADR-006/ADR-023: QuestPDF Community lisansı (ücretsiz) süreç genelinde bir kez ayarlanır.
        QuestPDF.Settings.License = LicenseType.Community;

        // PDF render (saf) ve dosya saklama (yerel disk) — stateless olduklarından Singleton (ARCHITECTURE_RULES.md §6.2).
        services.AddSingleton<IPolicyDocumentService, PolicyPdfDocumentService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Task 13: mock bildirim servisi (log/e-posta simülasyonu — ARCHITECTURE_RULES.md §6.1).
        services.AddScoped<INotificationService, MockNotificationService>();

        // Task 13: poliçe yaşam döngüsü arkaplan servisi (teklif/poliçe expiry + yenileme teklifi üretimi).
        services.AddHostedService<PolicyLifecycleBackgroundService>();

        return services;
    }
}
