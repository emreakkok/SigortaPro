using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class CoverageConfiguration : BaseEntityConfiguration<Coverage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Coverage> builder)
    {
        builder.ToTable("Coverages");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.DefaultLimit).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(c => c.InsuranceProduct)
            .WithMany(p => p.Coverages)
            .HasForeignKey(c => c.InsuranceProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.InsuranceProductId).HasDatabaseName("IX_Coverages_InsuranceProductId");
    }
}
