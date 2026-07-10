using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class PolicyConfiguration : BaseEntityConfiguration<Policy>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");

        builder.Property(p => p.PolicyNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.PolicyNumber).IsUnique().HasDatabaseName("UQ_Policies_PolicyNumber");

        builder.Property(p => p.TotalPremium).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Status).IsRequired();

        builder.HasOne(p => p.Customer)
            .WithMany(c => c.Policies)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Quote)
            .WithMany()
            .HasForeignKey(p => p.QuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CustomerId).HasDatabaseName("IX_Policies_CustomerId");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Policies_Status");
    }
}
