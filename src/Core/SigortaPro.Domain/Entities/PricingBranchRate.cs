using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

// ADR-048: Bir fiyatlandırma versiyonundaki tek branşın baz primi. Versiyonla birlikte değişmezdir;
// güncelleme metodu bilinçli olarak yoktur (yeni değer = yeni versiyon).
public class PricingBranchRate : BaseEntity
{
    protected PricingBranchRate()
    {
    }

    public PricingBranchRate(Guid pricingVersionId, InsuranceBranch branch, decimal basePremium)
    {
        if (basePremium <= 0m)
        {
            throw new DomainException("Baz prim sıfırdan büyük olmalıdır.");
        }

        Id = Guid.NewGuid();
        PricingVersionId = pricingVersionId;
        Branch = branch;
        BasePremium = basePremium;
    }

    public Guid PricingVersionId { get; private set; }
    public PricingVersion? PricingVersion { get; private set; }
    public InsuranceBranch Branch { get; private set; }

    // Branşın yıllık baz primi (TRY). Risk çarpanları bu değerin üzerine uygulanır.
    public decimal BasePremium { get; private set; }

    // Yalnızca TASLAK versiyon düzenlenirken PricingVersion.UpdateDraft üzerinden çağrılır (aktif/arşiv
    // versiyonun oranları asla değişmez — üst aggregate Draft guard'ı uygular). Satır yerinde güncellenir
    // (sil+ekle yerine) → aynı (versiyon, branş) benzersiz anahtarında çakışma oluşmaz.
    internal void SetBasePremium(decimal basePremium)
    {
        if (basePremium <= 0m)
        {
            throw new DomainException("Baz prim sıfırdan büyük olmalıdır.");
        }

        BasePremium = basePremium;
    }
}
