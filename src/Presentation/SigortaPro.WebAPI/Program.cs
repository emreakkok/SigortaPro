using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SigortaPro.Application;
using SigortaPro.Infrastructure;
using SigortaPro.Persistence;
using SigortaPro.Persistence.Context;
using SigortaPro.Persistence.Identity;
using SigortaPro.Persistence.Seed;
using SigortaPro.WebAPI.Extensions;

// Bootstrap logger: host kurulmadan önceki (yapılandırma yükleme, DI kurulum) hataları da loglanabilsin.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("SigortaPro API başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u host'a bağla; asıl yapılandırma appsettings.json "Serilog" bölümünden okunur (ADR-011).
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    builder.Services.AddControllers();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddWebApiServices(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var dbContext = scopedServices.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext);

        var userManager = scopedServices.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await IdentitySeeder.SeedAsync(userManager, roleManager);
    }

    // Çapraz kesit pipeline: korelasyon → güvenlik header'ları → istek loglama → global exception handling.
    app.UseSigortaProCrossCutting();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // HSTS yalnızca üretimde; localhost geliştirmede tarayıcı cache sorunlarını önlemek için kapalı.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseRouting();
    app.UseCors(WebApiConstants.CorsPolicyName);
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapSigortaProHealthChecks();

    // ADR-041: Gerçek zamanlı bildirim hub'ı (hub sınıfı Infrastructure'da; uç nokta composition root'ta).
    app.MapHub<SigortaPro.Infrastructure.RealTime.NotificationHub>("/hubs/notifications");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "SigortaPro API beklenmeyen şekilde sonlandı.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Integration test projesinin (WebApplicationFactory) erişebilmesi için partial Program sınıfı.
public partial class Program;
