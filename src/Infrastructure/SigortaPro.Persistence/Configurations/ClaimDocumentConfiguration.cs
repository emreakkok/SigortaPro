using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

// Hasar belgesi metadata tablosu (baytlar IFileStorageService'te). FK ilişkisi ClaimConfiguration'da tanımlıdır.
public sealed class ClaimDocumentConfiguration : BaseEntityConfiguration<ClaimDocument>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClaimDocument> builder)
    {
        builder.ToTable("ClaimDocuments");

        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(d => d.FileSizeBytes).IsRequired();

        builder.HasIndex(d => d.ClaimId).HasDatabaseName("IX_ClaimDocuments_ClaimId");
    }
}
