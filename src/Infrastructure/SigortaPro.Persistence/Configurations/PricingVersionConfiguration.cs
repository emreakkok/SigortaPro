using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

// ADR-048: Versiyonlanmış tarife. Sorgular hep "verilen ana yürürlükteki versiyon" olduğundan
// EffectiveFrom index'lenir; VersionNumber benzersizdir (kullanıcıya gösterilen sıra numarası).
public sealed class PricingVersionConfiguration : BaseEntityConfiguration<PricingVersion>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PricingVersion> builder)
    {
        builder.ToTable("PricingVersions");

        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.EffectiveFrom).IsRequired();
        builder.Property(version => version.Note).HasMaxLength(300);
        builder.Property(version => version.CreatedByName).HasMaxLength(120);

        builder.HasIndex(version => version.VersionNumber)
            .IsUnique()
            .HasDatabaseName("IX_PricingVersions_VersionNumber");
        builder.HasIndex(version => version.EffectiveFrom)
            .HasDatabaseName("IX_PricingVersions_EffectiveFrom");

        builder.HasMany(version => version.Rates)
            .WithOne(rate => rate.PricingVersion!)
            .HasForeignKey(rate => rate.PricingVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PricingBranchRateConfiguration : BaseEntityConfiguration<PricingBranchRate>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PricingBranchRate> builder)
    {
        builder.ToTable("PricingBranchRates");

        builder.Property(rate => rate.Branch).IsRequired();
        builder.Property(rate => rate.BasePremium).HasPrecision(18, 2).IsRequired();

        // Bir versiyonda bir branş yalnızca bir kez tanımlanabilir.
        builder.HasIndex(rate => new { rate.PricingVersionId, rate.Branch })
            .IsUnique()
            .HasDatabaseName("IX_PricingBranchRates_Version_Branch");
    }
}
