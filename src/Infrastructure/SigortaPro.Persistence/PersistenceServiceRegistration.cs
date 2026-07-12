using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Persistence.Context;
using SigortaPro.Persistence.Identity;
using SigortaPro.Persistence.Interceptors;
using SigortaPro.Persistence.Repositories;

namespace SigortaPro.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("'DefaultConnection' bağlantı dizesi appsettings.json içinde tanımlı değil.");
        }

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        // ADR-014: ASP.NET Core Identity, AppUser/IdentityRole üzerinde EF store'larıyla kurulur.
        // Cookie/SignInManager gerekmez; JWT akışı UserManager üzerinden yürür (AddIdentityCore yeterli).
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped(typeof(IReadRepository<>), typeof(GenericRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IInsuranceProductRepository, InsuranceProductRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IRenewalRepository, RenewalRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
