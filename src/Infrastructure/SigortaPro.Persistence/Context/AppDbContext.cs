using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SigortaPro.Domain.Entities;
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

    // Refresh token'lar Identity'nin yanında Persistence'ta tutulur (ADR-014).
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity eşlemeleri önce uygulanır; ardından proje konfigürasyonları eklenir.
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
