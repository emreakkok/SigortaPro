using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class RenewalConfiguration : BaseEntityConfiguration<Renewal>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Renewal> builder)
    {
        builder.ToTable("Renewals");

        builder.Property(r => r.OfferedAt).IsRequired();
        builder.Property(r => r.IsAccepted).IsRequired();

        builder.HasOne(r => r.Policy)
            .WithMany(p => p.Renewals)
            .HasForeignKey(r => r.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.NewQuote)
            .WithMany()
            .HasForeignKey(r => r.NewQuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.PolicyId).HasDatabaseName("IX_Renewals_PolicyId");
    }
}
