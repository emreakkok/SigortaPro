using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Behaviors;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Notifications;
using SigortaPro.Application.Features.Pricing;
using SigortaPro.Application.Features.Quotes;

namespace SigortaPro.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddMediatR(config => config.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);

        // pipeline sırası: Validation → Logging → Performance → UnhandledException → Handler.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        // handler başarıyla tamamlandıktan sonra olay→bildirim kataloğundan gerçek zamanlı yayın
        // (handler'a en yakın halka; yalnızca başarılı akışta çalışır, hata durumunda next() zaten fırlatır).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RealTimeNotificationBehavior<,>));

        // bildirim yayın orkestrasyonu (kalıcı fan-out + canlı yayın) — Application servisi.
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        // bildirim bağlamı (işlemi yapan / ilgili müşteri) çözümleyicisi — mevcut Identity/Customer
        // soyutlamalarını yeniden kullanır; ayrı bir audit altyapısı değildir.
        services.AddScoped<INotificationContextResolver, NotificationContextResolver>();
        // fiyatlamada kullanılacak tarifeyi çözer (yeni fiyatlama → yürürlükteki;
        // mevcut teklifin yeniden hesabı → teklifin sabitlediği versiyon).
        services.AddScoped<IPricingRateResolver, PricingRateResolver>();

        // Fiyatlama girdisinin tek kurulum noktası — önizleme ile teklif oluşturmanın
        // aynı girdiyi (dolayısıyla aynı fiyatı) üretmesini yapısal olarak garanti eder.
        services.AddScoped<IQuotePricingInputBuilder, QuotePricingInputBuilder>();

        return services;
    }
}
