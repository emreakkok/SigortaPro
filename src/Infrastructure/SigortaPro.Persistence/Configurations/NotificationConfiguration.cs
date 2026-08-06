using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

// Kalıcı bildirimler. Zil/merkez sorguları hep "alıcıya göre en yeniler" olduğundan
// (RecipientUserId, CreatedAt) bileşik index'i; okunmamış sayacı için ReadAt da eklenir.
public sealed class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Severity).HasMaxLength(10).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.RelatedEntityType).HasMaxLength(50);

        // (additive, nullable): actor snapshot'ı ve operasyonel referans.
        builder.Property(n => n.ActorName).HasMaxLength(120);
        builder.Property(n => n.ReferenceCode).HasMaxLength(60);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_Recipient_CreatedAt");
        builder.HasIndex(n => new { n.RecipientUserId, n.ReadAt })
            .HasDatabaseName("IX_Notifications_Recipient_ReadAt");
    }
}
