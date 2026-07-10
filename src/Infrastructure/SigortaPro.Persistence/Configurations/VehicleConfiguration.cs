using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class VehicleConfiguration : BaseEntityConfiguration<Vehicle>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.Property(v => v.PlateNumber).HasMaxLength(15).IsRequired();
        builder.Property(v => v.Brand).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100).IsRequired();

        // Customer'dan başlayan birden fazla ilişki SQL Server'da çoklu cascade path hatası
        // verdiğinden (Vehicle, Property, Quote, Policy, Claim hepsi Customer'a bağlı), tüm
        // Customer ilişkileri Restrict olarak konfigüre edilir.
        builder.HasOne(v => v.Customer)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.CustomerId).HasDatabaseName("IX_Vehicles_CustomerId");
        builder.HasIndex(v => v.PlateNumber).HasDatabaseName("IX_Vehicles_PlateNumber");
    }
}
