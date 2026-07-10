using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

public sealed class PaymentConfiguration : BaseEntityConfiguration<Payment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.InstallmentCount).IsRequired();
        builder.Property(p => p.MaskedCardNumber).HasMaxLength(25).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.ProviderReferenceCode).HasMaxLength(100);
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Quote)
            .WithMany()
            .HasForeignKey(p => p.QuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CustomerId).HasDatabaseName("IX_Payments_CustomerId");
        builder.HasIndex(p => p.QuoteId).HasDatabaseName("IX_Payments_QuoteId");
    }
}
