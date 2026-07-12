using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class QuoteConfiguration : BaseEntityConfiguration<Quote>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes");

        builder.Property(q => q.Branch).IsRequired();
        builder.Property(q => q.Status).IsRequired();
        builder.Property(q => q.CoveragePackage).IsRequired();
        builder.Property(q => q.TotalPremium).HasColumnType("decimal(18,2)").IsRequired();

        // Yenileme hasar geçmişi çarpanı; mevcut/normal tekliflerde varsayılan 1.00 (ADR-025).
        builder.Property(q => q.ClaimHistoryFactor)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(1.00m)
            .IsRequired();

        builder.HasOne(q => q.Customer)
            .WithMany(c => c.Quotes)
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.InsuranceProduct)
            .WithMany()
            .HasForeignKey(q => q.InsuranceProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Vehicle)
            .WithMany()
            .HasForeignKey(q => q.VehicleId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(q => q.Property)
            .WithMany()
            .HasForeignKey(q => q.PropertyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(q => q.CustomerId).HasDatabaseName("IX_Quotes_CustomerId");
        builder.HasIndex(q => q.Status).HasDatabaseName("IX_Quotes_Status");
    }
}
