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

        // ADR-041: "Başkası adına" sağlık sigortalısı — Quotes tablosuna gömülü (OwnsOne) nullable kolonlar;
        // ayrı tablo/aggregate açılmaz (Address value object emsali). Ham TCKN yalnızca DB'de tutulur,
        // DTO'larda maskeli döner (CODING_STANDARDS §4.2).
        builder.OwnsOne(q => q.InsuredPerson, insured =>
        {
            insured.Property(p => p.FirstName).HasColumnName("InsuredFirstName").HasMaxLength(100);
            insured.Property(p => p.LastName).HasColumnName("InsuredLastName").HasMaxLength(100);
            insured.Property(p => p.Tckn).HasColumnName("InsuredTckn").HasMaxLength(11);
            insured.Property(p => p.BirthDate).HasColumnName("InsuredBirthDate");
            insured.Property(p => p.PhoneNumber).HasColumnName("InsuredPhoneNumber").HasMaxLength(20);
            insured.Property(p => p.Relationship).HasColumnName("InsuredRelationship").HasMaxLength(50);
        });

        // ADR-053: Fiyatlama girdisi snapshot'ı. Tüm kolonlar nullable — snapshot'sız (eski) kayıtlar
        // mevcut davranışı korur. Branşa göre yalnızca ilgili alanlar dolar.
        builder.OwnsOne(q => q.PricingSnapshot, snapshot =>
        {
            // Zorunlu alan: EF'in "snapshot var mı" ayrımını yapmasını sağlar (diğerleri branşa göre null).
            snapshot.Property(s => s.CapturedAt).HasColumnName("PricingCapturedAt");
            snapshot.Property(s => s.DriverAge).HasColumnName("PricingDriverAge");
            snapshot.Property(s => s.VehicleAge).HasColumnName("PricingVehicleAge");
            snapshot.Property(s => s.EnginePowerHp).HasColumnName("PricingEnginePowerHp");
            snapshot.Property(s => s.UsagePurpose).HasColumnName("PricingUsagePurpose");
            snapshot.Property(s => s.RiskCity).HasColumnName("PricingRiskCity").HasMaxLength(100);
            snapshot.Property(s => s.NoClaimTier).HasColumnName("PricingNoClaimTier");
            snapshot.Property(s => s.BuildingAge).HasColumnName("PricingBuildingAge");
            snapshot.Property(s => s.SquareMeters).HasColumnName("PricingSquareMeters");
            snapshot.Property(s => s.EarthquakeZone).HasColumnName("PricingEarthquakeZone");
            snapshot.Property(s => s.InsuredAge).HasColumnName("PricingInsuredAge");
            snapshot.Property(s => s.IsSmoker).HasColumnName("PricingIsSmoker");
        });
    }
}
