using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class InsuranceProductConfiguration : BaseEntityConfiguration<InsuranceProduct>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InsuranceProduct> builder)
    {
        builder.ToTable("InsuranceProducts");

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Branch).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
    }
}
