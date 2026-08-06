using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Persistence.Identity;

namespace SigortaPro.Persistence.Configurations;

// / AppUser'a eklenen iki alanın (IsActive, FullName) EF eşlemesi. Identity'nin
// base eşlemesi OnModelCreating'de önce uygulanır; bu konfigürasyon yalnızca yeni alanları tanımlar.
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Additive kolon: mevcut satırlar için DEFAULT 1 (aktif) → migration sonrası erişim korunur.
        builder.Property(user => user.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(user => user.FullName)
            .HasMaxLength(100);
    }
}
