using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class PropertyConfiguration : BaseEntityConfiguration<Property>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.Property(p => p.BuildingAge).IsRequired();
        builder.Property(p => p.SquareMeters).IsRequired();
        builder.Property(p => p.EarthquakeZone).IsRequired();

        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100).IsRequired();
            address.Property(a => a.District).HasColumnName("District").HasMaxLength(100).IsRequired();
            address.Property(a => a.Neighborhood).HasColumnName("Neighborhood").HasMaxLength(150).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(10).IsRequired();
        });

        builder.Navigation(p => p.Address).IsRequired();

        builder.HasOne(p => p.Customer)
            .WithMany(c => c.Properties)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CustomerId).HasDatabaseName("IX_Properties_CustomerId");
    }
}
