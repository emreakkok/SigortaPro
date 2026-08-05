using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SigortaPro.Domain.Entities;
using SigortaPro.Persistence.Configurations.Common;

namespace SigortaPro.Persistence.Configurations;

// ADR-048: Versiyonlanmış tarife. Yaşam döngüsü durumuna göre sorgulanır (aktif/taslak) → Status index'lenir;
// VersionNumber benzersizdir (kullanıcıya gösterilen sıra numarası).
public sealed class PricingVersionConfiguration : BaseEntityConfiguration<PricingVersion>
{
    // Ticari kaldıraç seti tek bir JSON kolonuna serileştirilir (owned value object → ayrı tablo açılmaz;
    // minimum şema değişikliği). Değişmez olduğundan snapshot referansı korunur; değişiklik tespiti JSON eşitliğiyle.
    private static readonly JsonSerializerOptions RuleSetJsonOptions = new();

    protected override void ConfigureEntity(EntityTypeBuilder<PricingVersion> builder)
    {
        builder.ToTable("PricingVersions");

        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.Name).HasMaxLength(120);
        builder.Property(version => version.EffectiveFrom).IsRequired();
        builder.Property(version => version.EffectiveTo);
        builder.Property(version => version.ActivatedAt);
        builder.Property(version => version.Note).HasMaxLength(300);
        builder.Property(version => version.CreatedByName).HasMaxLength(120);
        builder.Property(version => version.Status).IsRequired();

        var ruleSetConverter = new ValueConverter<PricingRuleSet?, string?>(
            ruleSet => ruleSet == null ? null : JsonSerializer.Serialize(ruleSet, RuleSetJsonOptions),
            json => string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PricingRuleSet>(json, RuleSetJsonOptions));

        var ruleSetComparer = new ValueComparer<PricingRuleSet?>(
            (left, right) => Serialize(left) == Serialize(right),
            ruleSet => ruleSet == null ? 0 : Serialize(ruleSet).GetHashCode(),
            ruleSet => ruleSet);

        builder.Property(version => version.RuleSet)
            .HasConversion(ruleSetConverter, ruleSetComparer)
            .HasColumnName("RuleSet");

        builder.HasIndex(version => version.VersionNumber)
            .IsUnique()
            .HasDatabaseName("IX_PricingVersions_VersionNumber");
        builder.HasIndex(version => version.Status)
            .HasDatabaseName("IX_PricingVersions_Status");

        builder.HasMany(version => version.Rates)
            .WithOne(rate => rate.PricingVersion!)
            .HasForeignKey(rate => rate.PricingVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static string Serialize(PricingRuleSet? ruleSet) =>
        ruleSet == null ? string.Empty : JsonSerializer.Serialize(ruleSet, RuleSetJsonOptions);
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
