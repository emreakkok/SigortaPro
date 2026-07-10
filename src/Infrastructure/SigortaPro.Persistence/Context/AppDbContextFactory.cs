using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SigortaPro.Persistence.Context;

// DEVELOPMENT_RULES.md §5.3: "dotnet ef" komutları Persistence projesinden çalıştırılır.
// Persistence'ın kendi appsettings.json'ı olmadığından (gerçek bağlantı dizesi WebAPI'de),
// bu factory yalnızca tasarım zamanında (migration üretimi) kullanılır. Bağlantı, makineye
// özgü bilgi sızdırmamak için önce `SIGORTAPRO_DESIGN_CONNECTION` ortam değişkeninden okunur;
// tanımlı değilse yerel varsayılan SQL Server Express instance'ına (makineden bağımsız `.` = localhost)
// düşer. Çalışma zamanı bağlantısı PersistenceServiceRegistration üzerinden gelir.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultDesignTimeConnectionString =
        "Server=.\\SQLEXPRESS;Database=SigortaProDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SIGORTAPRO_DESIGN_CONNECTION")
            ?? DefaultDesignTimeConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
