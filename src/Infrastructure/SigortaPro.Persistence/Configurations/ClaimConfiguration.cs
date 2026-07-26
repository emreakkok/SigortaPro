using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class ClaimConfiguration : BaseEntityConfiguration<Claim>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.Property(c => c.Description).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.EstimatedAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.ApprovedAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ReviewNote).HasMaxLength(1000);
        builder.Property(c => c.Status).IsRequired();

        builder.HasOne(c => c.Policy)
            .WithMany(p => p.Claims)
            .HasForeignKey(c => c.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Customer)
            .WithMany(cu => cu.Claims)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hasar belgeleri (foto/PDF) aggregate'in parçasıdır → hasarla birlikte silinir (cascade).
        builder.HasMany(c => c.Documents)
            .WithOne()
            .HasForeignKey(d => d.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.PolicyId).HasDatabaseName("IX_Claims_PolicyId");
        builder.HasIndex(c => c.CustomerId).HasDatabaseName("IX_Claims_CustomerId");
        builder.HasIndex(c => c.Status).HasDatabaseName("IX_Claims_Status");
    }
}
