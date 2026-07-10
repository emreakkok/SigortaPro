using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Persistence.Identity;

namespace SigortaPro.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Token).HasMaxLength(512).IsRequired();
        builder.HasIndex(token => token.Token).IsUnique().HasDatabaseName("UQ_RefreshTokens_Token");

        builder.Property(token => token.UserId).IsRequired();
        builder.HasIndex(token => token.UserId).HasDatabaseName("IX_RefreshTokens_UserId");

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.ReplacedByToken).HasMaxLength(512);

        builder.HasOne<AppUser>()
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
