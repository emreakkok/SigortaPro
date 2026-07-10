using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.Property(c => c.AppUserId).IsRequired();
        builder.HasIndex(c => c.AppUserId).IsUnique().HasDatabaseName("UQ_Customers_AppUserId");

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();

        builder.Property(c => c.Tckn).HasMaxLength(11).IsRequired();
        builder.HasIndex(c => c.Tckn).IsUnique().HasDatabaseName("UQ_Customers_TCKN");

        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();

        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100).IsRequired();
            address.Property(a => a.District).HasColumnName("District").HasMaxLength(100).IsRequired();
            address.Property(a => a.Neighborhood).HasColumnName("Neighborhood").HasMaxLength(150).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(10).IsRequired();
        });

        builder.Navigation(c => c.Address).IsRequired();
    }
}
