using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class PolicyDocumentConfiguration : BaseEntityConfiguration<PolicyDocument>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PolicyDocument> builder)
    {
        builder.ToTable("PolicyDocuments");

        builder.Property(d => d.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.GeneratedAt).IsRequired();

        builder.HasOne(d => d.Policy)
            .WithOne(p => p.PolicyDocument)
            .HasForeignKey<PolicyDocument>(d => d.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.PolicyId).IsUnique().HasDatabaseName("UQ_PolicyDocuments_PolicyId");
    }
}
