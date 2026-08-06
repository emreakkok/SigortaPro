using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Common;

namespace SigortaPro.Persistence.Configurations.Common;

// Tüm entity konfigürasyonlarının paylaştığı ortak alanları (Id, audit, soft-delete filtresi)
// merkezi olarak uygular; her entity konfigürasyonu yalnızca kendine özgü kuralları ekler.
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy).HasMaxLength(256);
        builder.Property(entity => entity.UpdatedBy).HasMaxLength(256);

        // Soft delete — fiziksel silme yok, global query filter ile varsayılan sorgulardan hariç tutulur.
        builder.HasQueryFilter(entity => !entity.IsDeleted);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);
}
