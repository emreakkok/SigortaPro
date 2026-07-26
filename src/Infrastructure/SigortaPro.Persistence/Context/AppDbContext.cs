using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Conversions;
using SigortaPro.Persistence.Identity;

namespace SigortaPro.Persistence.Context;

// ADR-014: AppDbContext, IdentityDbContext'ten türer; ASP.NET Core Identity tabloları (AspNetUsers,
// AspNetRoles vb.) Domain aggregate'leriyle aynı veritabanında yönetilir.
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Yalnızca aggregate root'lar DbSet olarak dışa açılır (ARCHITECTURE_RULES.md §4.2);
    // Coverage ve PolicyDocument kendi konfigürasyonlarıyla modele dahil edilir ancak
    // ilgili aggregate root'un (InsuranceProduct, Policy) navigation'ı üzerinden erişilir.
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InsuranceProduct> InsuranceProducts => Set<InsuranceProduct>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Renewal> Renewals => Set<Renewal>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // ADR-048: Versiyonlanmış tarife (baz primler). Versiyonlar değişmezdir; fiyat değişikliği yeni satır ekler.
    public DbSet<PricingVersion> PricingVersions => Set<PricingVersion>();
    public DbSet<PricingBranchRate> PricingBranchRates => Set<PricingBranchRate>();

    // Refresh token'lar Identity'nin yanında Persistence'ta tutulur (ADR-014).
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // TIMEZONE: Date-only semantiği taşıyan alanlar (takvim günü — saat/timezone anlamsız). Bu alanlar UTC
    // Kind işaretlemesinin DIŞINDA tutulur; böylece serileştirmede "Z" almaz ve gün kayması riski oluşmaz.
    private static readonly HashSet<string> DateOnlyPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(Customer.BirthDate),        // Domain: Customer.BirthDate
        nameof(InsuredPerson.BirthDate),   // Domain: InsuredPerson.BirthDate (aynı ad — "BirthDate")
    };

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity eşlemeleri önce uygulanır; ardından proje konfigürasyonları eklenir.
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyUtcDateTimeConversions(builder);
    }

    // TIMEZONE (T1): Tüm "instant" DateTime/DateTime? alanları okunurken Kind=Utc olarak işaretlenir →
    // API çıktısı ISO-8601 + "Z" üretir (13:23 → 10:23 kök nedeninin çözümü). Merkezîdir: her property'ye
    // tek tek elle dokunulmaz. Şema/veri değişmez (yalnızca materializasyon Kind'ı) → migration gerekmez.
    private static void ApplyUtcDateTimeConversions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // Date-only alanlar hariç (takvim günü timezone'a tabi değildir).
                if (DateOnlyPropertyNames.Contains(property.Name))
                {
                    continue;
                }

                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverters.Utc);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(UtcDateTimeConverters.NullableUtc);
                }
            }
        }
    }
}
