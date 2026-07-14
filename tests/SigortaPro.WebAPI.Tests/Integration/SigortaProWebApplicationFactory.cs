using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SigortaPro.Infrastructure.BackgroundJobs;
using SigortaPro.Persistence.Context;
using SigortaPro.Persistence.Identity;
using SigortaPro.Persistence.Interceptors;
using SigortaPro.Persistence.Seed;

namespace SigortaPro.WebAPI.Tests.Integration;

// Task 21 / ADR-034: Entegrasyon testleri için gerçek HTTP pipeline'ı (middleware, auth, MediatR, EF Core)
// SQLite in-memory veritabanıyla ayağa kaldıran WebApplicationFactory.
//
// - Ortam "Testing"tir: Program.cs'in Development'a özel migrate/seed bloğu ve Swagger devre dışı kalır;
//   şema EnsureCreated ile EF modelinden üretilir (SQL Server'a özgü migration'lar çalıştırılmaz).
// - Tek bir açık SqliteConnection factory ömrü boyunca tutulur (":memory:" veritabanı bağlantı kapanınca
//   silinir). Serilog bootstrap logger'ı süreçte tek kez dondurulabildiğinden TÜM entegrasyon sınıfları bu
//   factory'yi IntegrationTestCollection üzerinden paylaşır (bkz. oradaki açıklama); test izolasyonu her
//   testin kendi kullanıcı/verisini üretmesiyle sağlanır.
// - PolicyLifecycleBackgroundService kaldırılır: arkaplan komutları Task 13 birim testlerinde kapsanır ve
//   paylaşılan SQLite bağlantısına eşzamanlı erişim testleri kırılgan yapar.
// - Referans verisi (5 sigorta ürünü + roller) prodüksiyondaki seeder'larla yüklenir; testler kendi
//   kullanıcı/araç/teklif verisini kendileri oluşturur (DEVELOPMENT_RULES.md §5.4).
public sealed class SigortaProWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // HS256 en az 256 bit anahtar ister; yalnızca test ortamında kullanılır.
    private const string TestJwtSecretKey = "sigortapro-integration-tests-secret-key-0123456789-ABCDEF";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Minimal hosting'de (WebApplication.CreateBuilder) Program.cs konfigürasyonu servis kaydı sırasında
        // inline okur; ConfigureAppConfiguration geç kalır. UseSetting değerleri host konfigürasyonuna erken
        // girer ve fail-fast kontrollerinden önce görünür.
        // AddPersistenceServices boş bağlantı dizesinde fail-fast yapar; gerçek sağlayıcı aşağıda SQLite ile
        // değiştirildiğinden bağlantı dizesi değeri hiçbir zaman kullanılmaz.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(unused-in-tests)");
        builder.UseSetting("JwtSettings:SecretKey", TestJwtSecretKey);

        builder.ConfigureTestServices(services =>
        {
            ReplaceSqlServerWithSqlite(services);
            RemovePolicyLifecycleBackgroundService(services);
        });
    }

    /// <summary>Test host'u başlatıldıktan sonra şemayı oluşturur ve referans verisini seed eder.</summary>
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var dbContext = scopedServices.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(dbContext);

        var userManager = scopedServices.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await IdentitySeeder.SeedAsync(userManager, roleManager);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    // EF Core 8'de AddDbContext, options kurulumunu DbContextOptions<T> descriptor'ının factory'sinde taşır
    // ve TryAdd ile kaydeder; SQL Server kaydı sökülmeden ikinci AddDbContext çağrısı etkisiz kalırdı.
    // Bu yüzden mevcut kayıtlar kaldırılıp SQLite ile (aynı interceptor desenini koruyarak) yeniden kurulur.
    private void ReplaceSqlServerWithSqlite(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<AppDbContext>();

        _connection.Open();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(_connection);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });
    }

    private static void RemovePolicyLifecycleBackgroundService(IServiceCollection services)
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(PolicyLifecycleBackgroundService))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
