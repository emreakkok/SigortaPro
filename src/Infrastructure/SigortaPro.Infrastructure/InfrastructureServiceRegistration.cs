using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Infrastructure.Security;
using SigortaPro.Infrastructure.Services;

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

        return services;
    }
}
